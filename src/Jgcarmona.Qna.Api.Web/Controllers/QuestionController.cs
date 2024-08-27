﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Jgcarmona.Qna.Application.Features.Questions.Commands.CreateQuestion;
using NUlid;
using System.Security.Claims;
using Jgcarmona.Qna.Application.Features.Questions.Queries;

namespace Jgcarmona.Qna.Api.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuestionController : ControllerBase
    {
        private readonly IMediator _mediator;

        public QuestionController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize]
        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateQuestionModel model)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
            {
                return Unauthorized("User ID not found in token.");
            }
            var userNameClaim = User.FindFirst(ClaimTypes.Name)?.Value;

            var command = new CreateQuestionCommand
            {
                Model = model,
                AuthorId = Ulid.Parse(userIdClaim)
            };

            var result = await _mediator.Send(command);

            if (result == null)
                return BadRequest("Failed to create question");

            return Ok(result);
        }

        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            if (!Ulid.TryParse(id, out var questionId))
            {
                return BadRequest("Invalid Question ID format.");
            }

            var query = new GetQuestionByIdQuery { QuestionId = questionId };
            var question = await _mediator.Send(query);

            if (question == null)
            {
                return NotFound("Question not found.");
            }

            return Ok(question);
        }

        [Authorize]
        [HttpGet("by-moniker/{moniker}")]
        public async Task<IActionResult> GetByMoniker(string moniker)
        {
            var query = new GetQuestionByMonikerQuery { Moniker = moniker };
            var question = await _mediator.Send(query);

            if (question == null)
            {
                return NotFound("Question not found.");
            }

            return Ok(question);
        }
    }
}