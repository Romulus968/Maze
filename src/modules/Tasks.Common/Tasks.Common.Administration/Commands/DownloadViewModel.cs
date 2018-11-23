﻿using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using Tasks.Common.Administration.Commands.Utils;
using Tasks.Common.Shared.Commands;
using Tasks.Infrastructure.Administration.Library;
using Tasks.Infrastructure.Administration.Library.Command;
using Tasks.Infrastructure.Administration.PropertyGrid;
using Tasks.Infrastructure.Core;
using Unclassified.TxLib;

namespace Tasks.Common.Administration.Commands
{
    public class DownloadViewModel : PropertyGridViewModel, ICommandViewModel<DownloadCommandInfo>
    {
        public DownloadViewModel()
        {
            this.RegisterFileSource(FileSource);
            this.RegisterProperty(() => TargetPath, Tx.T("TasksCommon:Commands.Download.Properties.TargetPath"), Tx.T("TasksCommon:Commands.Download.Properties.TargetPath.Description"), Tx.T("TasksCommon:Categories.Common"));
        }

        public string TargetPath { get; set; }
        public FileSourceViewModel FileSource { get; set; }

        public DownloadCommandInfo Build()
        {
            return new DownloadCommandInfo
            {
                TargetPath = TargetPath,
                FileSource = FileSource.Build()
            };
        }

        public void Initialize(DownloadCommandInfo model)
        {
            FileSource.Initialize(model.FileSource);
            TargetPath = model.TargetPath;
        }

        public ValidationResult ValidateContext(OrcusTask orcusTask)
        {
            return ValidationResult.Success;
        }

        public ValidationResult ValidateInput()
        {
            var fileSourceResult = FileSource.ValidateInput();
            if (fileSourceResult != ValidationResult.Success)
                return fileSourceResult;

            if (string.IsNullOrWhiteSpace(TargetPath))
                return new ValidationResult("Target path cannot be empty");

            try
            {
                File.Exists(TargetPath);
            }
            catch (Exception)
            {
                return new ValidationResult("Invalid Target path");
            }

            return ValidationResult.Success;
        }
    }
}
