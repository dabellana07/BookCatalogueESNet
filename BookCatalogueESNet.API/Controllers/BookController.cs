using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using BookCatalogueESNet.API.DTO;
using BookCatalogueESNet.Contracts.ElasticSearch.Services;
using BookCatalogueESNet.ElasticSearch.Documents;
using Microsoft.AspNetCore.Mvc;

namespace BookCatalogueESNet.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookController : ControllerBase
    {
        private readonly IBookElasticService _bookElasticService;
        private readonly IMapper _mapper;

        public BookController(
            IBookElasticService bookElasticService,
            IMapper mapper)
        {
            _bookElasticService = bookElasticService;
            _mapper = mapper;
        }

        [HttpGet]
        [Route("search")]
        public async Task<IActionResult> Search(string term, string genre,
            string startDate, string endDate, int take=10)
        {
            DateTime? startDateParsed = null, endDateParsed = null;

            if (!string.IsNullOrEmpty(startDate))
                startDateParsed = DateTime.Parse(startDate);

            if (!string.IsNullOrEmpty(endDate))
                endDateParsed = DateTime.Parse(endDate);

            var bookDocuments = await _bookElasticService.SearchBooks(
                term, genre, startDateParsed, endDateParsed, take);
            return Ok(bookDocuments.Select(d => _mapper.Map<BookDTO>(d)).ToList());
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var book = await _bookElasticService.GetBook(id);

            if (book == null)
                return NotFound();

            return Ok(book);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody]BookDTO bookDTO)
        {
            var book = await _bookElasticService.AddBook(_mapper.Map<Book>(bookDTO));

            return CreatedAtAction(nameof(Get), new { id = book.Id },
                _mapper.Map<BookDTO>(book));
        }

        [HttpPut]
        [Route("{id}")]
        public async Task<IActionResult> Put(Guid id, [FromBody]BookDTO bookDTO)
        {
            if (bookDTO == null || id != bookDTO.Id)
                return BadRequest();    

            await _bookElasticService.UpdateBook(_mapper.Map<Book>(bookDTO));

            return NoContent();
        }

        [HttpDelete]
        [Route("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _bookElasticService.RemoveBook(id);

            return Ok();
        }
    }
}