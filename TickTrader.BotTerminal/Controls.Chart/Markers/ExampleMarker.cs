﻿using LiveChartsCore.SkiaSharpView.Drawing.Geometries;
using SkiaSharp;

namespace TickTrader.BotTerminal.Controls.Chart.Markers
{
    internal sealed class ExampleMarker : SVGPathGeometry
    {
        private static readonly SKPath _svgPath = SKPath.ParseSvgPathData("M512,242.326v-15.254l-23.815,0.071c-0.06,0-0.119,0-0.178,0c-15.225,0-29.539-5.916-40.324-16.67" +
            "c-7.153-7.132-12.167-15.826-14.756-25.316c24.747-1.038,44.563-21.487,44.563-46.486v-15.256l-21.866,0.068" +
            "c-0.043,0-0.083,0-0.126,0c-10.838,0-21.03-4.212-28.708-11.869c-5.331-5.315-9.003-11.846-10.762-18.974" +
            "c11.738-6.853,19.648-19.578,19.648-34.122V43.262l-20.381,0.064c-7.502,0.043-14.6-2.891-19.927-8.203" +
            "c-5.327-5.312-8.262-12.381-8.261-19.915V0H124.901L124.9,15.208c-0.001,15.495-12.607,28.101-28.099,28.102H76.325v15.209" +
            "c0,14.542,7.905,27.263,19.637,34.116c-4.41,17.683-20.421,30.827-39.449,30.828H34.511v15.209" +
            "c0,24.994,19.808,45.439,44.55,46.485c-6.66,24.158-28.814,41.96-55.058,41.962H0v15.209c0,29.04,22.317,52.954,50.698,55.523" +
            "v140.497h-8.897C18.751,438.346,0,457.097,0,480.147V512h512v-31.853c0-23.049-18.751-41.801-41.801-41.801h-8.897V297.849" +
            "C489.683,295.279,512,271.366,512,242.326z M153.312,30.417H358.67c2.627,9.842,7.811,18.86,15.22,26.246" +
            "c4.505,4.493,9.611,8.161,15.138,10.93H122.866C137.674,60.194,148.926,46.701,153.312,30.417z M72.196,152.137" +
            "c26.779-6.057,47.879-27.279,53.755-54.126h260.083c2.863,13.223,9.464,25.361,19.276,35.144c9.625,9.598,21.51,16.1,34.451,19.01" +
            "c-2.533,1.66-5.559,2.625-8.808,2.625h-0.062H81.12h-0.073C77.78,154.789,74.738,153.813,72.196,152.137z M461.304,468.763h8.895" +
            "c6.276,0,11.384,5.107,11.384,11.384v1.436H30.417v-1.436c0-6.276,5.107-11.384,11.384-11.384h8.897h73.655h38.661h73.655h38.661" +
            "h73.655h38.661H461.304z M81.116,438.346V298.089h12.821v140.257H81.116z M124.353,438.346V298.089h38.661v140.257H124.353z" +
            " M193.431,438.346V298.089h12.821v140.257H193.431z M236.669,438.346V298.089h38.661v140.257H236.669z M305.748,438.346V298.089" +
            "h12.821v140.257H305.748z M348.985,438.346V298.089h38.661v140.257H348.985z M418.064,438.346V298.089h12.821v140.257H418.064z" +
            " M456.237,267.672h-68.591h-38.661H275.33h-38.661h-73.655h-38.661H55.761c-8.578,0-16.174-4.285-20.761-10.826" +
            "c38.077-4.796,68.619-34.145,75.215-71.64h291.563c3.068,17.649,11.476,33.894,24.427,46.806" +
            "c13.899,13.857,31.617,22.473,50.773,24.865C472.389,263.401,464.803,267.672,456.237,267.672z");

        public ExampleMarker() : base(_svgPath)
        {
        }
    }
}
