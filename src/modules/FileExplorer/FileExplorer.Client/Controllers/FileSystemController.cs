﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FileExplorer.Client.Extensions;
using FileExplorer.Client.Utilities;
using FileExplorer.Shared.Dtos;
using Microsoft.AspNetCore.Http;
using Orcus.Modules.Api;
using Orcus.Modules.Api.Parameters;
using Orcus.Modules.Api.Routing;

namespace FileExplorer.Client.Controllers
{
    [Route("fileSystem")]
    public class FileSystemController : OrcusController
    {
        [OrcusGet]
        public async Task<IActionResult> QueryPathEntries([FromQuery] string path, [FromQuery] bool directoriesOnly = false)
        {
            var directoryHelper = new DirectoryHelper();

            IList<FileExplorerEntry> entries;
            if (directoriesOnly)
            {
                entries = (await directoryHelper.GetDirectoryEntries(path, CancellationToken.None))
                    .ToList<FileExplorerEntry>();
            }
            else
            {
                entries = (await directoryHelper.GetEntries(path, CancellationToken.None)).ToList();
            }

            return Ok(entries);
        }

        [OrcusGet("path")]
        public IActionResult ExpandEnvironmentVariables([FromQuery] string path) =>
            Ok(Environment.ExpandEnvironmentVariables(path));

        [OrcusGet("directory")]
        public IActionResult GetDirectory([FromQuery] string path)
        {
            using (var directory = new DirectoryInfoEx(path))
            {
                var directoryEntry = new DirectoryHelper().GetDirectoryEntry(directory, null);
                return Ok(directoryEntry);
            }
        }

        [OrcusPost("directory")]
        public IActionResult CreateDirectory([FromQuery] string path)
        {
            Directory.CreateDirectory(path);
            return StatusCode(StatusCodes.Status201Created);
        }

        [OrcusDelete("directory")]
        public IActionResult DeleteDirectory([FromQuery] string path)
        {
            Directory.Delete(path, true);
            return Ok();
        }

        [OrcusDelete("file")]
        public IActionResult DeleteFile([FromQuery] string path)
        {
            System.IO.File.Delete(path);
            return Ok();
        }

        [OrcusPatch("file")]
        public IActionResult MoveFile([FromQuery] string path, [FromQuery] string newPath)
        {
            System.IO.File.Move(path, newPath);
            return Ok();
        }

        [OrcusPatch("directory")]
        public IActionResult MoveDirectory([FromQuery] string path, [FromQuery] string newPath)
        {
            Directory.Move(path, newPath);
            return Ok();
        }
    }
}