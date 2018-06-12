﻿using System.IO;
using Microsoft.AspNetCore.Http;
using Orcus.Modules.Api.Request;

namespace Orcus.Server.OrcusSockets.Internal.Http
{
    internal class DecodedOrcusRequest : OrcusRequest
    {
        public override string Method { get; set; }
        public override PathString Path { get; set; }
        public override QueryString QueryString { get; set; }
        public override IQueryCollection Query { get; set; }
        public override IHeaderDictionary Headers { get; set; }

        public override long? ContentLength
        {
            get => Headers.ContentLength;
            set => Headers.ContentLength = value;
        }

        public override string ContentType
        {
            get => Headers["Content-Type"];
            set => Headers["Content-Type"] = value;
        }

        public override Stream Body { get; set; }
    }
}