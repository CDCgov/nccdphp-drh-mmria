using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using mmria.common.model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension; 
using mmria.common.SharedLibraries.NIOSH.Manager;

namespace mmria.server;

[Route("api/[controller]")]
public sealed class nioshController: ControllerBase
{ 
    public record Record_Id_Response
    {
        public bool ok { get; init;}
        public bool is_unique { get; init;}
    }    
    private readonly NIOSHManager _nioshManager;
    public nioshController
    (
        NIOSHManager nioshManager
    )
    {
        _nioshManager = nioshManager;
    }

    [HttpGet]
    public async Task<mmria.common.niosh.NioshResult> Get(string o = null, string i = null)
    {
        return await _nioshManager.GetCodesAsync(o, i);
    } 
} 


