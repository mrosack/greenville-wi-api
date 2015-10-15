﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GreenvilleWiApi.Data.GarbageCollection;
using GreenvilleWiApi.Data.GoogleGeocoding;
using Microsoft.AspNet.Mvc;
using Swashbuckle.Swagger;
using Swashbuckle.Swagger.Annotations;

namespace GreenvilleWiApi.WebApi5.Controllers
{
    /// <summary>
    /// Garbage collection API
    /// </summary>
    [Route("api/[controller]")]
    public class GarbageCollectionController : Controller
    {
        /// <summary>
        /// Gets the current time in the Central time zone
        /// </summary>
        private DateTime CentralDateTime
        {
            get
            {
                // There doesn't seem to be ConvertTimeFromUtc in .NET Core, so we'll wing it for now...
                ////return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time"));

                return DateTime.UtcNow.AddHours(-6);
            }
        }

        /// <summary>
        /// Gets the upcoming garbage collection dates, with a range optionally passed in
        /// </summary>
        /// <param name="addr">The address of the property to get collection dates for</param>
        /// <param name="startDate">The start date of the range to provide collection dates for (if null, defaults to today)</param>
        /// <param name="endDate">The end date of the range to provide collection dates for (if null, defaults to one month in the future)</param>
        /// <returns>A list of collection dates</returns>
        [HttpGet]
        [SwaggerOperationFilter(typeof(GarbageCollectionOperationFilter))]
        public async Task<IEnumerable<GarbageCollectionEvent>> Get(string addr, DateTime? startDate = null, DateTime? endDate = null)
        {
            if (startDate < this.CentralDateTime || startDate == null)
                startDate = this.CentralDateTime;

            if (endDate > this.CentralDateTime.AddMonths(3) || endDate == null)
                endDate = startDate.Value.AddMonths(1);

            var address = (
                from a in await GoogleGeocoder.Geocode(addr)
                from c in a.Components
                from t in c.Types
                where t == "postal_code" && c.LongName == "54942"
                select a).FirstOrDefault();

            if (address != null)
            {
                var garbageDay = GarbageDayCalculator.CalculateGarbageDay(address.Geometry.Location);
                return GarbageDayCalculator.CalculateCollectionEvents(garbageDay, startDate.Value, endDate.Value);
            }

            return new List<GarbageCollectionEvent>();
        }

        /// <summary>
        /// Operation filter for the garbage collection API
        /// </summary>
        public class GarbageCollectionOperationFilter : IOperationFilter
        {
            /// <summary>
            /// Called when generating the operation
            /// </summary>
            public void Apply(Operation operation, OperationFilterContext context)
            {
                operation.OperationId = "GarbageCollection_Get";

                foreach (var dtParam in operation.Parameters.Where(x => x is NonBodyParameter).Cast<NonBodyParameter>())
                {
                    // TODO: Make this less hacky when possible!
                    if (dtParam.Name == "addr")
                        dtParam.Required = true;
                }
            }
        }
    }
}
