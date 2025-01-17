﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using Microsoft.ML.Calibrators;
using Microsoft.ML.Data;
using Microsoft.ML.PerformanceTests.Harness;
using Microsoft.ML.Trainers;

namespace Microsoft.ML.PerformanceTests
{
    [CIBenchmark]
    public class KMeansAndLogisticRegressionBench : BenchmarkBase
    {
        private readonly string _dataPath = GetBenchmarkDataPathAndEnsureData("adult.tiny.with-schema.txt");

        [Benchmark]
        public CalibratedModelParametersBase<LinearBinaryModelParameters, PlattCalibrator> TrainKMeansAndLR()
        {
            var ml = new MLContext(seed: 1);
            // Pipeline

            var input = ml.Data.LoadFromTextFile(_dataPath, new[] {
                            new TextLoader.Column("Label", DataKind.Boolean, 0),
                            new TextLoader.Column("CatFeatures", DataKind.String,
                                new [] {
                                    new TextLoader.Range() { Min = 1, Max = 8 },
                                }),
                            new TextLoader.Column("NumFeatures", DataKind.Single,
                                new [] {
                                    new TextLoader.Range() { Min = 9, Max = 14 },
                                }),
            }, hasHeader: true);

            var estimatorPipeline = ml.Transforms.Categorical.OneHotEncoding("CatFeatures")
                .Append(ml.Transforms.NormalizeMinMax("NumFeatures"))
                .Append(ml.Transforms.Concatenate("Features", "NumFeatures", "CatFeatures"))
                .Append(ml.Clustering.Trainers.KMeans("Features"))
                .Append(ml.Transforms.Concatenate("Features", "Features", "Score"))
                .Append(ml.BinaryClassification.Trainers.LbfgsLogisticRegression(
                    new LbfgsLogisticRegressionBinaryTrainer.Options { EnforceNonNegativity = true, OptimizationTolerance = 1e-3f, }));

            var model = estimatorPipeline.Fit(input);
            // Return the last model in the chain.
            return model.LastTransformer.Model;
        }
    }
}
