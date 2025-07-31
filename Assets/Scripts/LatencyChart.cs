using UnityEngine;
using XCharts.Runtime;

public class LatencyChart : MonoBehaviour, ISubscriber<double[]>
{
    public LineChart lineChart;
    
    void Start()
    {
        if(!lineChart) lineChart = gameObject.GetComponent<LineChart>();
        lineChart.RemoveData();
        var line = lineChart.AddSerie<Line>("i");
        line.AnimationEnable(false);
        var yAxis = lineChart.EnsureChartComponent<YAxis>();
        yAxis.minMaxType = Axis.AxisMinMaxType.Custom;
        yAxis.min = 0;
        yAxis.max = 100;
    }
    
    public void SubscriptionUpdate(double[] message)
    {
        lineChart.ClearData();
        
        for (int i = 0; i < message.Length; i++)
        {
            lineChart.AddXAxisData(i.ToString());
            lineChart.AddData(0, message[i]);
        }
    }
}
