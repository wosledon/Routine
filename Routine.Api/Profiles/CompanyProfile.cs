using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Routine.Api.Entities;
using Routine.Api.Models;

namespace Routine.Api.profiles
{
    public class CompanyProfile: Profile
    {
        public CompanyProfile()
        {
            CreateMap<Company, CompanyDto>()
                // 手动映射 Entity Model-> Model
                 .ForMember(
                    dest => dest.CompanyName,
                    opt => opt.MapFrom(src => src.Name));

            CreateMap<CompanyAddDto, Company>();
        }
    }
}
