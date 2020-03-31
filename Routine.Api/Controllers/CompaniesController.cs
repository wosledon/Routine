using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Routine.Api.DtoParameters;
using Routine.Api.Entities;
using Routine.Api.Models;
using Routine.Api.Services;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly IPropertyMappingService _propertyMappingService;
        private readonly IPropertyCheckerService _propertyCheckerService;

        public CompaniesController(
            ICompanyRepository companyRepository, 
            IMapper mapper,
            IPropertyMappingService propertyMappingService,
            IPropertyCheckerService propertyCheckerService)
        {
            _companyRepository = companyRepository ??
                                 throw new ArgumentNullException(nameof(companyRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _propertyMappingService = propertyMappingService 
                                      ?? throw new ArgumentNullException(nameof(propertyMappingService));
            _propertyCheckerService = propertyCheckerService;
        }

        [HttpGet(Name = nameof(GetCompanies))]
        [HttpHead] // 不返回body, 只返回响应
        // public async Task<IActionResult> GetCompanies()
        public async Task<IActionResult> GetCompanies(
            [FromQuery]CompanyDtoParameters parameters)
        {
            if (!_propertyMappingService.ValidMappingExistsFor<CompanyDto, Company>(parameters.OrderBy))
            {
                return BadRequest();
            }

            if (!_propertyCheckerService.TypeHasProperties<CompanyDto>(parameters.Fields))
            {
                return BadRequest();
            }

            var companies = await _companyRepository.GetCompaniesAsync(parameters);

            //// 翻页
            //var previousPageLink = companies.HasPrevious
            //    ? CreateCompaniesResourceUri(parameters, ResourceUriType.PreviousPage)
            //    : null;

            //var nextPageLink = companies.HasNext
            //    ? CreateCompaniesResourceUri(parameters, ResourceUriType.NextPage)
            //    : null;

            var paginationMetadata = new
            {
                totalCount = companies.TotalCount,
                pageSize = companies.PageSize,
                currentPage = companies.CurrentPage,
                totalPages = companies.TotalPages,
                //previousPageLink,
                //nextPageLink
            };

            Response.Headers.Add("X-Pagination", JsonSerializer.Serialize(paginationMetadata, 
                new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                }));

            var companyDtos = _mapper.Map<IEnumerable<CompanyDto>>(companies);
            var shapedData = companyDtos.ShapeData(parameters.Fields);

            var links = CreateLinksForCompany(parameters, companies.HasPrevious, companies.HasNext);

            var shapedCompaniesWithLinks = shapedData.Select(c =>
            {
                var companyDict = c as IDictionary<string, object>;
                var companyLinks = CreateLinksForCompany((Guid) companyDict["Id"], null);
                companyDict.Add("links", companyDict);
                return companyDict;
            });

            var linkedCollectionResource = new
            {
                value = shapedCompaniesWithLinks,
                links
            };

            return Ok(linkedCollectionResource);
        }

        [HttpGet("{companyId}", Name = nameof(GetCompany))]
        public async Task<IActionResult> GetCompany(Guid companyId, string fields)
        {
            //// 并发请求较大时, 有瑕疵
            //var exist = await _companyRepository.CompanyExistsAsync(companyId);
            //if (!exist)
            //{
            //    return NotFound();
            //}
            if (!_propertyCheckerService.TypeHasProperties<CompanyDto>(fields))
            {
                return BadRequest();
            }

            var company = await _companyRepository.GetCompanyAsync(companyId);
            if (company == null)
            {
                return NotFound();
            }

            var links = CreateLinksForCompany(companyId, fields);

            var linkedDict = _mapper.Map<CompanyDto>(company).ShapeData(fields)
                as IDictionary<string, object>;

            linkedDict.Add("links", links);

            return Ok(linkedDict);
        }

        [HttpPost(Name = nameof(CreateCompany))]
        public async Task<ActionResult<CompanyDto>> CreateCompany([FromBody]CompanyAddDto company)
        {
            var entity = _mapper.Map<Company>(company);
            _companyRepository.AddCompany(entity);
            await _companyRepository.SaveAsync();

            var returnDto = _mapper.Map<CompanyDto>(entity);

            var links = CreateLinksForCompany(returnDto.Id, null);

            var linkedDict = returnDto.ShapeData(null)
                as IDictionary<string, object>;
            linkedDict.Add("links", links);

            return CreatedAtRoute(nameof(GetCompany), new {companyId = linkedDict["Id"]}, linkedDict);
        }

        [HttpDelete("{companyId}", Name = nameof(DeleteCompany))]
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
                        field = parameters.Fields,
                        orderBy = parameters.OrderBy,
                        pageNumber = parameters.PageNumber - 1,
                        pageSize = parameters.PageSize,
                        companyName = parameters.CompanyName,
                        searchTerm = parameters.SearchTerm
                    });
                case ResourceUriType.NextPage:
                    return Url.Link(nameof(GetCompanies), new
                    {
                        field = parameters.Fields,
                        orderBy = parameters.OrderBy,
                        pageNumber = parameters.PageNumber + 1,
                        pageSize = parameters.PageSize,
                        companyName = parameters.CompanyName,
                        searchTerm = parameters.SearchTerm
                    });
                case ResourceUriType.CurrentPage:
                default:
                    return Url.Link(nameof(GetCompanies), new
                    {
                        field = parameters.Fields,
                        orderBy = parameters.OrderBy,
                        pageNumber = parameters.PageNumber,
                        pageSize = parameters.PageSize,
                        companyName = parameters.CompanyName,
                        searchTerm = parameters.SearchTerm
                    });
            }
        }

        private IEnumerable<LinkDto> CreateLinksForCompany(Guid companyid, string fields)
        {
            var links = new List<LinkDto>();

            if (string.IsNullOrWhiteSpace(fields))
            {
                links.Add(
                    new LinkDto(Url.Link(nameof(GetCompany), new {companyid}),
                    "self",
                    "GET"));
            }
            else
            {
                links.Add(
                    new LinkDto(Url.Link(nameof(GetCompany), new { companyid, fields }),
                    "self",
                    "GET"));
            }

            links.Add(
                new LinkDto(Url.Link(nameof(DeleteCompany), new { companyid }),
                    "delete_company",
                    "DELETE"));

            links.Add(
                new LinkDto(Url.Link(nameof(EmployeesController.CreateEmployeeForCompany),new { companyid }),
                    "create_employee_for_company",
                    "POST"));

            links.Add(
                new LinkDto(Url.Link(nameof(EmployeesController.GetEmployeesForCompany), new { companyid }),
                    "employees",
                    "GET"));
            return links;
        }

        private IEnumerable<LinkDto> CreateLinksForCompany(
            CompanyDtoParameters parameters,
            bool hasPrevious,
            bool hasNext)
        {
            var links = new List<LinkDto>();

            links.Add(new LinkDto(CreateCompaniesResourceUri(parameters, ResourceUriType.CurrentPage),
                "self",
                "GET"));

            if (hasPrevious)
            {
                links.Add(new LinkDto(CreateCompaniesResourceUri(parameters, ResourceUriType.PreviousPage),
                    "previous_page",
                    "GET"));
            }

            if (hasNext)
            {
                links.Add(new LinkDto(CreateCompaniesResourceUri(parameters, ResourceUriType.NextPage),
                    "next_page",
                    "GET"));
            }

            return links;
        }
    }
}
