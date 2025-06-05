using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using EMBC.ESS.Shared.Contracts;
using EMBC.ESS.Shared.Contracts.Events;
using EMBC.Utilities.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using EMBC.Utilities.Hosting;

namespace EMBC.Responders.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class SupportsController : ControllerBase
{
    private readonly IMessagingClient messagingClient;
    private readonly IMapper mapper;
    private readonly IConfiguration configuration;

    public SupportsController(IMessagingClient messagingClient, IMapper mapper, IConfiguration configuration)
    {
        this.messagingClient = messagingClient ?? throw new ArgumentNullException(nameof(messagingClient));
        this.mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        this.configuration = configuration;
    }

    /// <summary>  
    /// Checks if a support being issued is a potential duplicate  
    /// </summary>  
    /// <param name="request">Details about the support being checked for duplicates</param>  
    /// <returns>Potential duplicate supports</returns>  
    [HttpGet("get-duplicate-supports")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDuplicateSupports([FromQuery] CheckDuplicateSupportsRequest request)
    {
        var response = await messagingClient.Send(new DuplicateSupportsQuery
        {
            Category = request.Category,
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            Members = request.Members,
            FileId = request.FileId,
            IssuedBy = request.IssuedBy
        });

        var sameFileDuplicateDetectionEnabled = configuration.GetValue(FeatureFlagKeys.SameFileDuplicateDetection, false);
        var differentFileDuplicateDetectionEnabled = configuration.GetValue(FeatureFlagKeys.DifferentFileDuplicateDetection, false);
        var originalDuplicatedSupports = response.DuplicateSupports.ToList(); // Convert to List for AddRange support
        var duplicatedSupports = new List<DuplicateSupportResult>();
        if (sameFileDuplicateDetectionEnabled)
        {
            duplicatedSupports = originalDuplicatedSupports
                .Where(ds => ds.duplicateSupportScenario == (int)ConflictMessageScenario.ExactMatchSameFile)
                .ToList();
        }

        if (differentFileDuplicateDetectionEnabled)
        {
            duplicatedSupports.AddRange(originalDuplicatedSupports
                .Where(ds => ds.duplicateSupportScenario == (int)ConflictMessageScenario.ExactMatchOnDifferentEssFile)
                .ToList());
        }
        response.DuplicateSupports = duplicatedSupports;
        return Ok(response.DuplicateSupports);
    }
}

public record CheckDuplicateSupportsRequest
{
    public string Category { get; set; }
    public string FromDate { get; set; }
    public string ToDate { get; set; }
    public string[] Members { get; set; }
    public string FileId { get; set; }
    public string IssuedBy { get; set; }
}
public enum ConflictMessageScenario
{
    ExactMatchSameFile = 174360000,
    ExactMatchOnDifferentEssFile = 174360001,
    PartialMatchSameFile = 174360002,
    PartialMatchOnDifferentEssFile = 174360003
}
