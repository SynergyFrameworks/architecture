At start up, TypeMapper.Initialize needs to be called:

TypeMapper.Initialize("DapperTestProj.Entities");
And you can start using attributes for the entity properties

using DapperTestProj.DapperAttributeMapper;

namespace DapperTestProj.Entities
{
    public class Table1
    {
        [Column("Table1Id")]
        public int Id { get; set; }

        public string Column1 { get; set; }

        public string Column2 { get; set; }

        public Table2 Table2 { get; set; }

        public Table1()
        {
            Table2 = new Table2();
        }
    }
}
Cornelis

Popular Answer
Cornelis's answer is correct, however I wanted to add an update to this. As of the current version of Dapper you also need to implement SqlMapper.ItypeMap.FindExplicitConstructor(). I'm not sure when this change was made, but this for anyone else that stumbles upon this question and is missing that part of the solution.

Within FallbackTypeMapper.cs

public ConstructorInfo FindExplicitConstructor()
{
    return _mappers.Select(m => m.FindExplicitConstructor())
        .FirstOrDefault(result => result != null);
}
Also you can use the ColumnAttribute class located within the System.ComponentModel.DataAnnotations.Schema namespace instead of rolling your own for build-in non-database/orm specific version.