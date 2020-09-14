using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Drawing;
using log4net;
using MML.Enterprise.Common.Mapping;
using MML.Enterprise.Common.Extensions;
using MML.Enterprise.Excel;

namespace MML.Enterprise.Common.DataFile
{
    public class ExcelTransformer : AbstractDataFileTransformer
    {
        public IExcelManager ExcelManager { get; set; }
        private ILog Log = LogManager.GetLogger(typeof (ExcelTransformer));

        public override Stream TransformToFile(Mapping.Mapping mapping, IList<Dictionary<string, object>> data,
                                      IDictionary<string, object> parameters)
        {
            if (data.Count != 1)
                throw new NotSupportedException();
            var workbook = parameters.ContainsKey("template") ? ExcelManager.OpenWorkbook((Stream)parameters["template"]) : ExcelManager.OpenWorkbook(Path.GetTempFileName());
            var buildHeaderRow = parameters.ContainsKey("buildHeaderRow") && (bool)parameters["buildHeaderRow"];
            var refs = (Dictionary<string, ImportReferencedataList>) (parameters.ContainsKey("referenceData") ? parameters["referenceData"] : null);
            if(buildHeaderRow)
                WriteHeaderRow(workbook, mapping.PropertyMappings.Where(p => p.IsHeaderMapping).ToList());
            if (refs != null && refs.Count > 0)
            {
                WriteRefDataSheets(refs, workbook);
            }
            WriteObject(workbook, data[0], mapping.PropertyMappings, 0,mapping.SubstituteYesForTrue, refs:refs);


            //clean up unfilled cells
            for (var i = 1; i <= workbook.Worksheets.Count; i++)
            {
                var worksheet = workbook.Worksheets[i];
                var cells = worksheet.FindWithValue("###");
                foreach (var cell in cells)
                {
                    worksheet.Cells[cell.Key].Value = "";
                }
                worksheet.UsedRange?.AutoFitColumns();
            }
            var path = Path.GetTempFileName();
            workbook.SaveAs(path); 
            return new FileStream(path, FileMode.Open);
        }

        public override Stream TransformToFile(IList data, IList<string> properties = null)
        {
            var path = Path.GetTempFileName();            
            var workbook = ExcelManager.OpenWorkbook(path);


            if (data.Count == 0)
            {
                workbook.SaveAs(path);
                return new FileStream(path, FileMode.Open);
            }
            var props = data[0].GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();
            if (properties != null && properties.Count > 0)
            {
                props = (from p in properties
                        join prop in props
                            on p.Substring(0, p.IndexOf(".") > 0 ? p.IndexOf(".") : p.Length) equals prop.Name
                        select prop).ToList();
            }
            else
                props = props.OrderBy(p => p.Name).ToList();

            //Write Header Row
            var colCount = 1;            
            var worksheet = workbook.Worksheets[1];
            foreach (var prop in props)
            {
                var cell = worksheet.Cells[1, colCount];
                cell.Value = prop.Name;
                cell.Style.Bold = true;                
                cell.Style.Fill.Color = Color.Khaki;
                
                colCount++;
            }            

            //Write objects
            var rowCount = 2;
            colCount = 1;
            foreach (var item in data)
            {
                foreach (var prop in props)
                {
                    var value = item.GetPropertyValue(properties == null ? prop.Name : properties[colCount - 1]);
                    if (value is DateTime)
                    {
                        value = ((DateTime) value).ToOADate();
                        worksheet.Cells[rowCount, colCount].Style.NumberFormat = "mm/dd/yyyy";
                    }
                    worksheet.Cells[rowCount, colCount].Value = value;
                    colCount++;
                }
                colCount = 1;
                rowCount++;
            }
            worksheet.UsedRange?.AutoFitColumns();
            workbook.SaveAs(path);
            return new FileStream(path, FileMode.Open);
        }

        #region ReferenceData
        private void WriteRefDataSheets(Dictionary<string, ImportReferencedataList> refs, IWorkbook workbook)
        {
            foreach (var keyValuePair in refs)
            {
                var refData = keyValuePair.Value;
                var worksheet = workbook.AddWorksheet(refData.ReferenceDataType.Name);
                var list = refData.ReferenceData.ConvertList<ReferenceData>();
                var endIndex = list.Count + 1;
                worksheet.Cells[1, 1].Value = "Description";
                worksheet.Cells[1, 2].Value = "Id";
                for (int i = 2; i <= endIndex; i++)
                {
                    worksheet.Cells[i, 1].Value = list[i-2].Description;
                    worksheet.Cells[i, 2].Value = list[i -2].Id;
                }
            }
        }
        #endregion

        #region Writing

        public void WriteHeaderRow(IWorkbook workbook, IList<PropertyMapping> properties)
        {
            foreach (var property in properties)
            {
                GetRange(workbook, property).Value = property.HeaderName;
            }

        }

        public void WriteObject(IWorkbook workbook, IDictionary<string, object> data,
                                 IList<PropertyMapping> properties, int rowOffset, bool SubstituteYesForTrue = false, bool writingList = false, int colOffset = 0, string workSheet = null, Dictionary<string, ImportReferencedataList> refs = null)
        {

            List<string> worksheetsMapped;
            if (!string.IsNullOrEmpty(workSheet))
            {
                worksheetsMapped = new List<string> { workSheet };
            }
            else
            {
                worksheetsMapped = properties.Where(p => p.MappingInfo != null).Select(p => p.MappingInfo.Split('!')[0]).Distinct().ToList();
                worksheetsMapped.AddRange(properties.Where(p => p.PropertyMappings != null && p.PropertyMappings.Any()).SelectMany(p => p.PropertyMappings.Where(pm => pm.MappingInfo != null).Select(pm => pm.MappingInfo.Split('!')[0])));
                worksheetsMapped = worksheetsMapped.Distinct().ToList();
            }
            var isRefNull = refs == null || refs.Count == 0;
            foreach (var sheetName in worksheetsMapped)
            {
                var listsToMap = new Dictionary<IList<PropertyMapping>, IList>();
                foreach (var item in data)
                {
                    //grab all the property mappings that match
                    var property = properties.FirstOrDefault(p => p.PropertyName == item.Key && IsForSheet(sheetName, p));

                    if (property == null) continue;
                    if (property.PropertyMappings != null && property.PropertyMappings.Any())
                    {
                        var value = item.Value as Dictionary<string, object>;
                        if (value != null)
                        {
                            WriteObject(workbook, value, property.PropertyMappings, rowOffset, SubstituteYesForTrue, writingList, colOffset, sheetName, refs);
                        }
                        var key = item.Value as IList;
                        if (key != null)
                        {
                            var propertyPrefix = property.PropertyName;
                            var subPropertyList = property.PropertyMappings.Where(p => IsForSheet(sheetName, p)).ToList();
                            foreach (var subProperty in subPropertyList)
                            {
                                subProperty.PropertyPrefix = propertyPrefix;
                            }
                            listsToMap.Add(subPropertyList, key);
                        }
                    }
                    else
                    {
                        GetRange(workbook, property, rowOffset, colOffset).Value = SubstituteYesForTrue && item.Value is bool ? (((bool)item.Value) ? "Yes" : "") : FormatValue(property.Format, item.Value);

                        //here we are writing the dropdown list, based on the ref data
                        var refDataPropertyName = $"{property.PropertyPrefix}.{property.PropertyName}";
                        if (!isRefNull && refs.ContainsKey(refDataPropertyName))
                        {
                            var refData = refs[refDataPropertyName];
                            if (refData.HasReferenceData)
                            {
                                //Excel sheet name limit is 31 Chars. here we will crop the property name if needed.
                                var refSheetName = refData.ReferenceDataType.Name.Length > 31 ? refData.ReferenceDataType.Name.Substring(0, 31) : refData.ReferenceDataType.Name;
                                var formula = $"={refSheetName}!$A$2:$A${refData.ReferenceData.Count()+1}";
                                var sheet = workbook.Worksheets[sheetName.Replace("'", "")];
                                sheet.AddDataValidation($"{property.Column}{property.RowNumber+ rowOffset}", formula);
                            }
                        }
                    }
                }
                if (writingList) //if we're already writing a list we only support the second list to be column based. 
                {
                    foreach (var list in listsToMap)
                    {
                        var colListMapping = list.Key;
                        if (colListMapping.Count > 2)
                            throw new NotSupportedException("Invalid number of sublist mappings, can only support two (header and value)");
                        colListMapping = colListMapping.OrderBy(c => c.MappingInfo).ToList();
                        foreach (IDictionary<string, object> item in list.Value)
                        {
                            if (rowOffset == 0 && colListMapping.Count > 1) //if we're writing the first row, make sure we output the headers too
                            {
                                var headerMapping = colListMapping.FirstOrDefault();
                                if (headerMapping.PropertyMappings != null && headerMapping.PropertyMappings.Any())
                                {
                                    WriteObject(workbook, item.ContainsKey(headerMapping.PropertyName) ? (IDictionary<string, object>)item[headerMapping.PropertyName] : null, headerMapping.PropertyMappings, rowOffset, SubstituteYesForTrue, true, colOffset);
                                }
                                else
                                {
                                    GetRange(workbook, headerMapping, rowOffset, colOffset).Value = item.ContainsKey(headerMapping.PropertyName) ? item[headerMapping.PropertyName] : "";
                                }

                            }
                            var mapping = colListMapping.Count == 1 ? colListMapping.FirstOrDefault() : colListMapping[1];
                            if (mapping.PropertyMappings != null && mapping.PropertyMappings.Any())
                            {
                                WriteObject(workbook, item.ContainsKey(mapping.PropertyName) ? (IDictionary<string, object>)item[mapping.PropertyName] : null, mapping.PropertyMappings, rowOffset, SubstituteYesForTrue, true, colOffset);
                            }
                            else
                            {
                                GetRange(workbook, mapping, rowOffset, colOffset).Value = item.ContainsKey(mapping.PropertyName) ? item[mapping.PropertyName] : "";
                            }
                            colOffset++;
                        }
                    }

                }
                else
                {
                    WriteLists(workbook, listsToMap, SubstituteYesForTrue, refs);
                }
            }
        }

        public void WriteLists(IWorkbook workbook, Dictionary<IList<PropertyMapping>,IList> lists,bool SubstituteYesForTrue = false, Dictionary<string, ImportReferencedataList> refs = null)
        {            
            var offset = 0;
            var ordered = lists.OrderBy(l => l.Key.FirstOrDefault().RowNumber);
            foreach (var list in ordered)
            {
                var listMapping = list.Key.FirstOrDefault().MappingInfo;
                var mappingParts = listMapping.Split('!');
                var worksheet = workbook.Worksheets[mappingParts[0].Replace("'", "")];
                var startRow = worksheet.Cells[mappingParts[1]].StartRow + offset;
                if (list.Value.Count > 1)
                    worksheet.InsertRow(startRow, list.Value.Count - 1);
                foreach (var item in list.Value)
                {
                    WriteObject(workbook, (IDictionary<string, object>)item, list.Key , offset,SubstituteYesForTrue,true,0,mappingParts[0], refs);
                    offset++;
                }
                offset--;
            }            

        }

         
        private bool IsForSheet(string sheetName,PropertyMapping mapping)
        {
            return (mapping.MappingInfo != null && mapping.MappingInfo.Split('!')[0]== sheetName) || (mapping.MappingInfo == null && mapping.PropertyMappings != null && mapping.PropertyMappings.Any(p=> IsForSheet(sheetName,p)));
        }
        #endregion

        #region DictionaryReading
        public override Dictionary<string, object> TransformFromFile(string mapping, Stream file)
        {
            var result = new Dictionary<string, object>();

            return result;
        }

        public override Dictionary<string, object> TransformFromFile(Mapping.Mapping mapping, Stream file)
        {
            var workbook = ExcelManager.OpenWorkbook(file);
            var objects = mapping.PropertyMappings.Where(m => m.PropertyMappings != null && m.PropertyMappings.Any()).ToList();

            var listOffsets = new List<KeyValuePair<int, int>>();
            var result = objects.ToDictionary(obj => obj.PropertyName, obj => ReadObjectToDictionary(obj.PropertyMappings, workbook, listOffsets));

            var properties = mapping.PropertyMappings.Where(m => m.PropertyMappings == null || !m.PropertyMappings.Any());
            foreach (var property in properties)
            {
                result.Add(property.PropertyName, GetRange(workbook, property, GetRowOffset(listOffsets, property, workbook)).Value);
            }
            return result;
        }

        public object ReadObjectToDictionary(IList<PropertyMapping> mappings, IWorkbook workbook, IList<KeyValuePair<int, int>> listOffsets)
        {
            var obj = new Dictionary<string, object>();
            var listResponse = new List<Dictionary<string, object>>();
            var tryList = IsList(mappings.Where(m => !string.IsNullOrEmpty(m.MappingInfo)).Select(m => m.MappingInfo));
            var rowCnt = 0;
            while (tryList || rowCnt == 0)
            {
                foreach (var mapping in mappings)
                {
                    if (mapping.PropertyMappings != null && mapping.PropertyMappings.Any())
                    {
                        obj.Add(mapping.PropertyName, ReadObjectToDictionary(mapping.PropertyMappings, workbook, listOffsets));
                    }
                    else
                    {
                        obj.Add(mapping.PropertyName,
                                GetRange(workbook, mapping,
                                         GetRowOffset(listOffsets, mapping, workbook) + rowCnt).Value);
                    }
                }
                if (tryList)
                {
                    tryList = CheckObjContent(obj);
                    if (tryList)
                    {
                        listResponse.Add(obj);
                        obj = new Dictionary<string, object>();
                    }
                }
                rowCnt++;
            }

            if (rowCnt > 1)
            {
                listOffsets.Add(new KeyValuePair<int, int>(GetRange(workbook,
                                                                     mappings.FirstOrDefault(
                                                                         m => !string.IsNullOrEmpty(m.MappingInfo))
                                                                             ,
                                                                     GetRowOffset(listOffsets,
                                                                                  mappings.FirstOrDefault(
                                                                                      m =>
                                                                                      !string.IsNullOrEmpty(
                                                                                          m.MappingInfo))
                                                                                          , workbook))
                                                                .StartRow, rowCnt - 2));
            }

            if (listResponse.Count > 0)
                return listResponse;
            return obj;
        }

        /// <summary>
        /// Helper method to try to figure out if we've reached the end of a list or not. Currently depends on 30% of the values being filled
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private bool CheckObjContent(Dictionary<string, object> obj)
        {
            var itemsWithValues = obj.Values.Count(v => v != null && !string.IsNullOrEmpty(v.ToString())) * 1.0;
            return (itemsWithValues / obj.Count) > .3;

        }

        #endregion

        #region ObjectReading
        /// <summary>
        /// Currently we're only assuming a basic list of columns that will be mapped to object properties on one worksheet
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="mapping"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        public override IList<T> TransformFromFile<T>(Mapping.Mapping mapping, Stream file)
        {
            var workbook = ExcelManager.OpenWorkbook(file);                                   
            var properties = mapping.PropertyMappings.Where(m => m.PropertyMappings == null || !m.PropertyMappings.Any());
            var results = new List<T>();
            //For the moment the first column needs to be filled
            var tempMapping = mapping.PropertyMappings.FirstOrDefault();
            var firstColString = tempMapping.SheetName + "!A" + tempMapping.RowNumber;
            var rowOffset = 0;
            var firstColValue = GetRange(workbook, new PropertyMapping {MappingInfo = firstColString}).Value;
            while (firstColValue != null && !string.IsNullOrEmpty(firstColValue.ToString()))
            {
                results.Add(ReadObject<T>(mapping.PropertyMappings,workbook,rowOffset));
                rowOffset++;
                firstColValue = GetRange(workbook, new PropertyMapping { MappingInfo = firstColString },rowOffset).Value;
            }
            return results;                    
        }

        /// <summary>
        /// List properties of a mapped objected are assumed to be at the end column wise
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="mappings"></param>
        /// <param name="workbook"></param>
        /// <param name="rowOffset"></param>
        /// <returns></returns>
        public T ReadObject<T>(IList<PropertyMapping> mappings, IWorkbook workbook, int rowOffset)
        {
            var obj = Activator.CreateInstance<T>();
            var objType = typeof (T);

            foreach (var propertyMapping in mappings)
            {

                var objProperty = objType.GetProperty(propertyMapping.PropertyName);
                if (objProperty == null) 
                    continue;

                if (objProperty.PropertyType.GetInterfaces().Contains(typeof(IEnumerable)) && objProperty.PropertyType.GetGenericArguments().Any())
                {
                    var listType = objProperty.PropertyType.GetGenericArguments().FirstOrDefault();
                    var listMethod = GetType().GetMethod("ReadColumnList", BindingFlags.NonPublic | BindingFlags.Instance).MakeGenericMethod(new[] { listType });
                    
                    objProperty.SetValue(obj,listMethod.Invoke(this,new object[] {workbook,propertyMapping,rowOffset}));
                    continue;                    
                }

                //if we have a complex object
                if (propertyMapping.PropertyMappings != null && propertyMapping.PropertyMappings.Any())
                {
                    var method = GetType().GetMethod("ReadObject").MakeGenericMethod(new[] {objProperty.PropertyType});
                    objProperty.SetValue(obj,method.Invoke(this, new object[] {propertyMapping.PropertyMappings, workbook, rowOffset}));
                    continue;
                }

                var objValue = GetRange(workbook, propertyMapping, rowOffset).Value;
                if (objValue == null)
                    continue;
                try
                {
                    if (objProperty.PropertyType == typeof (DateTime))
                    {
                        if (string.IsNullOrWhiteSpace(objValue.ToString()))
                            continue;
                        if (objValue is DateTime)
                            objProperty.SetValue(obj, objValue);
                        else if (objValue is double)
                            objProperty.SetValue(obj, DateTime.FromOADate((double) objValue));
                        else if (objValue is string)
                            objProperty.SetValue(obj, DateTime.Parse((string) objValue));
                    }
                    else if (objProperty.PropertyType == typeof (Guid))
                    {
                        objProperty.SetValue(obj, Guid.Parse((string) objValue));
                    }
                    else
                    {
                        objProperty.SetValue(obj, Convert.ChangeType(objValue, objProperty.PropertyType));
                    }
                }
                catch (Exception ex)
                {
                 Log.ErrorFormat("Error setting poroperty {0}, value {1}. {2}",objProperty.Name,objValue,ex);   
                }
            }
            return obj;
        }

        private IList<T> ReadColumnList<T>(IWorkbook workbook, PropertyMapping mapping, int rowOffset)
        {
            var response = new List<T>();
            //Assume the first mapping based on row number is the header
            var headerMapping = mapping.PropertyMappings.OrderBy(pm=> pm.RowNumber).FirstOrDefault();
            var valueMapping = mapping.PropertyMappings.OrderByDescending(pm => pm.RowNumber).FirstOrDefault();
            var colOffset = 0;
            var headerValue = GetRange(workbook, headerMapping).Value;
            while (headerValue != null && !string.IsNullOrEmpty(headerValue.ToString()))
            {
                var obj = Activator.CreateInstance<T>();
                var objProperty = obj.GetType().GetProperty(valueMapping.PropertyName);
                if (objProperty == null)
                    throw new NotSupportedException();
                obj.SetProperty(headerMapping.PropertyName,headerValue);

                var objValue = GetRange(workbook, valueMapping,rowOffset,colOffset).Value;
                if (objProperty.PropertyType == typeof (DateTime))
                {
                    objProperty.SetValue(obj, DateTime.FromOADate((double)objValue));
                }
                else if (objProperty.PropertyType == typeof (Guid))
                {
                    objProperty.SetValue(obj, Guid.Parse((string) objValue));
                }
                else
                {
                    objProperty.SetValue(obj, Convert.ChangeType(objValue, objProperty.PropertyType));
                }
                colOffset++;
                headerValue = GetRange(workbook, headerMapping, 0, colOffset).Value;
                response.Add(obj);
            }
            return response;
        }

        private T ReadColumnListObject<T>(IWorkbook workbook, PropertyMapping mapping, int rowOffset, int colOffset)
        {
            var obj = Activator.CreateInstance<T>();


            return obj;
        }
        
        #endregion 

        #region Helpers

        private IRange GetRange(IWorkbook workbook, PropertyMapping propertyMapping, int rowOffset=0, int colOffset = 0)
        {
            IRange range;
            if (propertyMapping.MappingInfo.Contains("!"))
            {

                var address = propertyMapping.MappingInfo.Split('!');
                var sheet = address[0].Replace("'", "");
                var worksheet = workbook.Worksheets[sheet];
                if (worksheet == null)
                    throw new InvalidDataException("No worksheet found with name " + sheet);
                range = worksheet.Cells[address[1]];
                return workbook.Worksheets[sheet].Cells[range.StartRow + rowOffset, range.StartCol + colOffset];
            }
            range = workbook.Worksheets[0].Cells[propertyMapping.MappingInfo];
            return workbook.Worksheets[0].Cells[range.StartRow + rowOffset, range.StartCol + colOffset];
        }

        /// <summary>
        /// Returns the number of rows a given property needs to be offset due to lists being populated
        /// </summary>
        /// <param name="listOffsets">A list of key value pairs contiaining the start row of a list and the number of rows in it</param>
        /// <param name="mappingInfo">The excel cell address of the mapping info </param>
        /// <returns></returns>
        public int GetRowOffset(IList<KeyValuePair<int, int>> listOffsets, PropertyMapping mapping, IWorkbook workbook)
        {
            var startRow = GetRange(workbook, mapping, 0).StartRow;

            return listOffsets.Where(listOffset => listOffset.Key < startRow).Sum(listOffset => listOffset.Value);
        }


        /// <summary>
        /// Indicates if we should try to treat an object as a list or not (all properties on one line)
        /// </summary>
        /// <param name="mappingInfos"></param>
        /// <returns></returns>
        public bool IsList(IEnumerable<string> mappingInfos)
        {
            if (!mappingInfos.Any())
                return false;
            var row = mappingInfos.FirstOrDefault().Substring(mappingInfos.FirstOrDefault().Length - 2);
            return mappingInfos.Count(m => m.EndsWith(row)) == mappingInfos.Count();
        }
    
        public string GetNestedProperty(IDictionary<string,object> item,string propertyName)
        {
            if (!propertyName.Contains("."))
                return item.ContainsKey(propertyName) ? item[propertyName].ToString() : "";

            var firstProp = propertyName.Substring(0,propertyName.IndexOf("."));
            if(item.ContainsKey(firstProp) && item[firstProp] is IDictionary<string,object>)
            {
                return GetNestedProperty((IDictionary<string,object>)item[firstProp], propertyName.Substring(propertyName.IndexOf(".")+1));
            }

            return "";
        }

        private object FormatValue(string format, object value)
        {
            return format == "DateTime" && value as DateTime? != null ? ((DateTime) value).ToShortDateString() : value;
        }
        #endregion
    }
}

