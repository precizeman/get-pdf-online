﻿using System;
using System.Collections.Generic;
using System.Text;

namespace PrecizeSoft.GetPdfOnline.Model
{
    public class ConvertStatByHour
    {
        public DateTimeOffset BeginRequestDateUtc { get; set; }

        public DateTimeOffset EndRequestDateUtc { get; set; }

        public int UtcMinutesOffset { get; set; }

        public int TotalCount { get; set; }

        public long FileSizeSum { get; set; }

        public long ResultFileSizeSum { get; set; }

        public long TotalFileSizeSum { get; set; }
    }
}
