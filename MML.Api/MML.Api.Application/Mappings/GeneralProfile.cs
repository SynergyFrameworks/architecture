using MML.Api.Application.Features.Products.Commands.CreateProduct;
using MML.Api.Application.Features.Products.Queries.GetAllProducts;
using AutoMapper;
using MML.Api.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace MML.Api.Application.Mappings
{
    public class GeneralProfile : Profile
    {
        public GeneralProfile()
        {
            CreateMap<Product, GetAllProductsViewModel>().ReverseMap();
            CreateMap<CreateProductCommand, Product>();
            CreateMap<GetAllProductsQuery, GetAllProductsParameter>();
        }
    }
}
