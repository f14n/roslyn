// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Interactive;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.VisualStudio.InteractiveWindow.Commands;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Interactive
{
    [Export(typeof(IInteractiveWindowCommand))]
    internal sealed class LoadCommand : InteractiveWindowCommand
    {
        private const string CommandName = "load";
        private readonly IStandardClassificationService _registry;

        [ImportingConstructor]
        public LoadCommand(IStandardClassificationService registry)
        {
            _registry = registry;
        }

        public override string Description
        {
            get { return "Executes the specified file within the current interactive session."; }
        }

        public override string Name
        {
            get { return CommandName; }
        }

        public override string CommandLine
        {
            get { return "\"path to .csx file\""; }
        }

        public override Task<ExecutionResult> Execute(IInteractiveWindow window, string arguments)
        {
            var engine = window.Evaluator as InteractiveEvaluator;
            if (engine != null)
            {
                int i = 0;
                string path;

                if (!CommandArgumentsParser.ParsePath(arguments, ref i, out path) || path == null)
                {
                    ReportInvalidArguments(window);
                    return ExecutionResult.Failed;
                }

                if (!CommandArgumentsParser.ParseTrailingTrivia(arguments, ref i))
                {
                    window.ErrorOutputWriter.WriteLine(string.Format(EditorFeaturesResources.UnexpectedText, arguments.Substring(i)));
                    return ExecutionResult.Failed;
                }

                return engine.LoadCommandAsync(path);
            }

            return ExecutionResult.Failed;
        }

        public override IEnumerable<ClassificationSpan> ClassifyArguments(ITextSnapshot snapshot, Span argumentsSpan, Span spanToClassify)
        {
            string path;
            var arguments = snapshot.GetText();
            int i = argumentsSpan.Start;

            int start, end;
            CommandArgumentsParser.ParsePath(arguments, ref i, out path, out start, out end);
            if (end > start)
            {
                yield return new ClassificationSpan(new SnapshotSpan(snapshot, Span.FromBounds(start, end)), _registry.StringLiteral);
            }

            CommandArgumentsParser.ParseTrailingTrivia(arguments, ref i, out start, out end);
            if (end > start)
            {
                yield return new ClassificationSpan(new SnapshotSpan(snapshot, Span.FromBounds(start, end)), _registry.Comment);
            }
        }
    }
}
