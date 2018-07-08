﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Trady.Core.Infrastructure;
using Trady.Core.Period;

using YahooFinanceApi;

namespace Trady.Importer.Yahoo
{
    public class YahooFinanceImporter : IImporter
    {
        private static readonly DateTime UnixMinDateTime = new DateTime(1901, 12, 13);
        private static readonly DateTime UnixMaxDateTime = new DateTime(2038, 1, 19);

        private static readonly TimeZoneInfo TzEst = TimeZoneInfo
            .GetSystemTimeZones()
            .Single(tz => tz.Id == "Eastern Standard Time" || tz.Id == "America/New_York");

        private static readonly IDictionary<PeriodOption, Period> PeriodMap = new Dictionary<PeriodOption, Period>
        {
            {PeriodOption.Daily, Period.Daily },
            {PeriodOption.Weekly, Period.Weekly },
            {PeriodOption.Monthly, Period.Monthly }
        };

        /// <summary>
        /// Imports the async. Endtime stock history exclusive
        /// </summary>
        /// <returns>The async.</returns>
        /// <param name="symbol">Symbol.</param>
        /// <param name="startTime">Start time.</param>
        /// <param name="endTime">End time.</param>
        /// <param name="period">Period.</param>
        /// <param name="token">Token.</param>
        public async Task<IReadOnlyList<IOhlcv>> ImportAsync(string symbol, DateTime? startTime = default(DateTime?), DateTime? endTime = default(DateTime?), PeriodOption period = PeriodOption.Daily, CancellationToken token = default(CancellationToken))
        {
            if (period != PeriodOption.Daily && period != PeriodOption.Weekly && period != PeriodOption.Monthly)
                throw new ArgumentException("This importer only supports daily, weekly & monthly data");

            var corrStartTime = ConvertLocalTimeToEst((startTime < UnixMinDateTime ? UnixMinDateTime : startTime) ?? UnixMinDateTime);
            var corrEndTime = ConvertLocalTimeToEst((endTime > UnixMaxDateTime ? UnixMaxDateTime : endTime) ?? UnixMaxDateTime);
            var candles = await YahooFinanceApi.Yahoo.GetHistoricalAsync(symbol, corrStartTime, corrEndTime, PeriodMap[period], token);

            return candles.Select(c => new Core.Candle(c.DateTime, c.Open, c.High, c.Low, c.Close, c.Volume)).OrderBy(c => c.DateTime).ToList();
        }

        private DateTime ConvertLocalTimeToEst(DateTime dateTime)
            => TimeZoneInfo.ConvertTime(dateTime, TimeZoneInfo.Local, TzEst);

        //private static DateTime AddPeriod(DateTime dateTime, PeriodOption period)
        //{
        //    switch (period)
        //    {
        //        case PeriodOption.Daily:
        //            return dateTime.AddDays(1);

        //        case PeriodOption.Weekly:
        //            return dateTime.AddDays(7);

        //        case PeriodOption.Monthly:
        //            return dateTime.AddMonths(1);

        //        default:
        //            return dateTime;
        //    }
        //}
    }
}
