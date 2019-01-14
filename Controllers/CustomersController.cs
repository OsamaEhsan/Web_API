using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Web_API.Dtos;
using Web_API.Entities;
using Web_API.Helpers;
using Web_API.Services;

namespace Web_API.Controllers
{
    [Authorize]
    [Route("[controller]")]
    [ApiController]
    public class CustomersController : ControllerBase
    {
        private ICustomerService _customerService;
        private IMapper _mapper;
        private readonly AppSettings _appSettings;


        public CustomersController(
            ICustomerService customerService,
            IMapper mapper,
            IOptions<AppSettings> appSettings)
        {
            _customerService = customerService;
            _mapper = mapper;
            _appSettings = appSettings.Value;
        }
        [AllowAnonymous]
        [HttpPost("register")]
        public IActionResult Register([FromBody]CustomerDto customerDto)
        {
            var customer = _mapper.Map<Customer>(customerDto);
            try
            {
                _customerService.Create(customer, customerDto.password);
                return Ok();
            }
            catch (AppException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        
        [AllowAnonymous]
        [HttpPost("authenticate")]
        public IActionResult Authenticate([FromBody]CustomerDto customerDto)
        {
            var customer = _customerService.Authenticate(customerDto.username, customerDto.password);

            if (customer == null)
                return BadRequest(new { message = "Username or password is incorrect" });

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, customer.ID.ToString())
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);
            return Ok(new
            {
                Id = customer.ID,
                Username = customer.username,
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                Token = tokenString
            });
        }
        [HttpGet]
        public IActionResult GetAll()
        {
            var customer = _customerService.GetAll();
            var customerDtos = _mapper.Map<IList<CustomerDto>>(customer);
            return Ok(customerDtos);
        }
        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var customer = _customerService.GetById(id);
            var customerDto = _mapper.Map<CustomerDto>(customer);
            return Ok(customerDto);
        }
    }
}
