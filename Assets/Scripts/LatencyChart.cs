using System;
using System.Linq;
using UnityEngine;
using XCharts.Runtime;

public class LatencyChart : MonoBehaviour, ISubscriber<UpdateRateCache>
{
    public LineChart lineChart;
    private bool _isInitialized;
    private XAxis _xAxis;

    private void Start()
    {
        if(!lineChart) lineChart = gameObject.GetComponent<LineChart>();
        lineChart.RemoveData();
        _xAxis = lineChart.GetChartComponent<XAxis>();
        _xAxis.minMaxType = Axis.AxisMinMaxType.MinMax;
        _xAxis.interval = 1;
    }

    private void Initialize(UpdateRateCache cache)
    {
        _isInitialized = true;
        lineChart.RemoveData();
        foreach (var stream in cache.Streams)
        {
            var line = lineChart.AddSerie<Line>();
            line.serieName = stream.Key;
            line.AnimationEnable(false);
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
        ulong minSequenceId = ulong.MaxValue, maxSequenceId = 0;
        for (var streamIndex = 0; streamIndex < update.Streams.Count; streamIndex++)
        {
            var stream = update.Streams.ElementAt(streamIndex);
            var line = lineChart.GetSerie(streamIndex);
            for (var entryIndex = 0; entryIndex < stream.Value.Count; entryIndex++)
            {
                var entry = stream.Value[entryIndex];
                if(entry.SequenceId < minSequenceId) minSequenceId = entry.SequenceId;
                if(entry.SequenceId > maxSequenceId) maxSequenceId = entry.SequenceId;
                line.UpdateXYData(entryIndex, entry.SequenceId, entry.Rate);
            }
        }
        
        _xAxis.min = minSequenceId;
        _xAxis.max = maxSequenceId;
    }
}
