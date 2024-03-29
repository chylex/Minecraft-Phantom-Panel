﻿using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Phantom.Web.Layout;

[AllowAnonymous]
[IgnoreAntiforgeryToken]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public class ErrorModel : PageModel {
	public string? RequestId { get; set; }

	public ErrorModel(ILogger<ErrorModel> logger) {}

	public void OnGet() {
		RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
	}
}
