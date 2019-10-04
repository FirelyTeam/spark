using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Spark.Web.Controllers
{
	[Authorize(Policy = "RequireAdministratorRole")]
	public class AdminController : Controller {
			[HttpGet]
			public IActionResult Index()
			{
				return View();
			}
			
			[HttpGet]
			public IActionResult Maintenance()
			{
				return View();
			}
	}
}