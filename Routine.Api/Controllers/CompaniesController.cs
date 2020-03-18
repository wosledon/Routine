using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Routine.Api.DtoParameters;
using Routine.Api.Entities;
using Routine.Api.Models;
using Routine.Api.Services;
using System;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Routine.Api.Helpers;

namespace Routine.Api.Controllers
{
    [ApiController]
    [Route("api/companies")]
    // [Route("api/[controller]")]
    public class CompaniesController: ControllerBase
    {
        private readonly ICompanyRepository _companyRepository;
        private readonly IMapper _mapper;

        public CompaniesController(ICompanyRepository companyRepository, IMapper mapper)
        {
            _companyRepository = companyRepository ??
                                 throw new ArgumentNullException(nameof(companyRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        [HttpGet(Name = nameof(GetCompanies))]
        [HttpHead] // 不返回body, 只返回响应
        // public async Task<IActionResult> GetCompanies()
        public async Task<ActionResult<IEnumerable<CompanyDto>>> GetCompanies(
            [FromQuery]CompanyDtoParameters parameters)
        {
            var companies = await _companyRepository.GetCompaniesAsync(parameters);

            // 翻页
            var previousPageLink = companies.HasPrevious
                ? CreateCompaniesResourceUri(parameters, ResourceUriType.PreviousPage)
                : null;

            var nextPageLink = companies.HasNext
                ? CreateCompaniesResourceUri(parameters, ResourceUriType.NextPage)
                : null;

            var pagenationMetadata = new
            {
                totalCount = companies.TotalCount,
                pageSize = companies.PageSize,
                currentPage = companies.CurrentPage,
                totalPages = companies.TotalPages,
                previousPageLink,
                nextPageLink
            };

            Response.Headers.Add("X-Pageination", JsonSerializer.Serialize(pagenationMetadata, 
                new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                }));

            var companyDtos = _mapper.Map<IEnumerable<CompanyDto>>(companies);

            //var companyDtos = new List<CompanyDto>();

            //foreach (var company in companies)
            //{
            //    companyDtos.Add(new CompanyDto()
            //    {
            //        Id = company.Id,
            //        Name = company.Name
            //    });
            //}
            // 404 NotFound();
            // return Ok();
            // return companyDtos;
            return Ok(companyDtos);
        }

        [HttpGet("{companyId}", Name = nameof(GetCompany))]
        public async Task<ActionResult<CompanyDto>> GetCompany(Guid companyId)
        {
            //// 并发请求较大时, 有瑕疵
            //var exist = await _companyRepository.CompanyExistsAsync(companyId);
            //if (!exist)
            //{
            //    return NotFound();
            //}

            var company = await _companyRepository.GetCompanyAsync(companyId);
            if (company == null)
            {
                return NotFound();
            }

            return Ok(_mapper.Map<CompanyDto>(company));
        }

        [HttpPost]
        public async Task<ActionResult<CompanyDto>> CreateCompany([FromBody]CompanyAddDto company)
        {
            var entity = _mapper.Map<Company>(company);
            _companyRepository.AddCompany(entity);
            await _companyRepository.SaveAsync();

            var returnDto = _mapper.Map<CompanyDto>(entity);

            return CreatedAtRoute(nameof(GetCompany), new {companyId = returnDto.Id}, returnDto);
        }

        [HttpDelete("{companyId}")]
        public async Task<IActionResult> DeleteCompany(Guid companyId)
        {
            var companyEntity = await _companyRepository.GetCompanyAsync(companyId);

            if (companyEntity == null)
            {
                return NotFound();
            }

            await _companyRepository.GetEmployeesAsync(companyId, null);

            _companyRepository.DeleteCompany(companyEntity);
            await _companyRepository.SaveAsync();

            return NoContent();
        }

        [HttpOptions]
        public IActionResult GetCompaniesOptions()
        {
            Response.Headers.Add("Allow", "GET,POST,OPTIONS");
            return Ok();
        }

        private string CreateCompaniesResourceUri(CompanyDtoParameters parameters, ResourceUriType type)
        {
            switch (type)
            {
                case ResourceUriType.PreviousPage:
                    return Url.Link(nameof(GetCompanies), new
                    {
                        pageNumber = parameters.PageNumber - 1,
                        pageSize = parameters.PageSize,
                        companyName = parameters.CompanyName,
                        searchTerm = parameters.SearchTerm
                    });
                case ResourceUriType.NextPage:
                    return Url.Link(nameof(GetCompanies), new
                    {
                        pageNumber = parameters.PageNumber + 1,
                        pageSize = parameters.PageSize,
                        companyName = parameters.CompanyName,
                        searchTerm = parameters.SearchTerm
                    });
                default:
                    return Url.Link(nameof(GetCompanies), new
                    {
                        pageNumber = parameters.PageNumber,
                        pageSize = parameters.PageSize,
                        companyName = parameters.CompanyName,
                        searchTerm = parameters.SearchTerm
                    });
            }
        }
    }
}
