using System.Linq;
using UnityEngine;
using XCharts.Runtime;

public class LatencyChart : MonoBehaviour, ISubscriber<UpdateRateCache>
{
    public LineChart lineChart;
    private bool _isInitialized;
    
    void Start()
    {
        if(!lineChart) lineChart = gameObject.GetComponent<LineChart>();
        lineChart.RemoveData();
        var yAxis = lineChart.EnsureChartComponent<YAxis>();
        yAxis.minMaxType = Axis.AxisMinMaxType.Custom;
        yAxis.min = 0;
        yAxis.max = 100;
    }

    void Initialize(UpdateRateCache cache)
    {
        _isInitialized = true;
        Debug.Log("Init latency chart: " + cache.Capacity);
        lineChart.RemoveData();
        foreach (var stream in cache.Streams)
        {
            var line = lineChart.AddSerie<Line>();
            line.serieName = stream.Key;
            var startingData = Enumerable.Range(0, cache.Capacity).Select(i => (double)i).ToArray();
            foreach (var data in startingData)
            {
                line.AddData(line.data.Count - 1, data);
            }
        }
    }
    
    public void SubscriptionUpdate(UpdateRateCache update)
    {
        if(!_isInitialized) Initialize(update);

        for (var streamIndex = 0; streamIndex < update.Streams.Count; streamIndex++)
        {
            var stream = update.Streams.ElementAt(streamIndex);
            var line = lineChart.GetSerie(streamIndex);
            for (var entryIndex = 0; entryIndex < stream.Value.Count; entryIndex++)
            {
                var entry = stream.Value[entryIndex];
                line.UpdateXYData(entryIndex, entry.SequenceId, entry.Rate);
            }
        }
    }
}
