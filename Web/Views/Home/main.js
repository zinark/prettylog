var dataPie, dataColumn;
var piechart, columnchart;

//var dateFormatter = new google.visualization.DateFormat({ pattern: 'MM/dd' });
//dateFormatter.format(dataTable, dateColumnIndex);


google.load('visualization', '1', { 'packages': ['corechart', 'table', 'controls'] });
google.setOnLoadCallback(drawChart);

function drawChart()
{
    var dashboard = new google.visualization.Dashboard(document.getElementById('divDash'));

    var chart = new google.visualization.ChartWrapper({
        'chartType': 'ColumnChart',
        'containerId': 'divChart',
        'options': {
            'legend': 'none'
        }
    });

    var controlTimestamp = new google.visualization.ControlWrapper({
        'controlType': 'ChartRangeFilter',
        'containerId': 'divControlTimestamp',
        'options': {
            'filterColumnLabel': 'Timestamp'
        },
        'ui': {
            'chartType': 'ComboChart',
            'chartOptions': {
                'chartArea': { 'width': '90%' },
                'hAxis': { 'baselineColor': 'none' },
                'seriesType': 'bars',
                'isStacked': true
            },
            // Display a single series that shows the closing value of the sales.
            // Thus, this view has two columns: the date (axis) and the stock value (line series).
            'chartView': {
                'columns': [1],
            }
        },
        'state': { 'range': { 'start': moment().subtract('days', 7).toDate(), 'end': moment().toDate() } }
    });

    $("#btnRanges").click(function ()
    {
        var r = controlTimestamp.getState().range;
        var h = r.start + " - " + r.end;

        $("#divRanges").html(h);
    });

    dashboard.bind(controlTimestamp, chart);
    dashboard.draw(data.columnJson);

    drawBasics();
}

function drawBasics()
{
    dataColumn = new google.visualization.DataTable(data.columnJson);
    dataPie = new google.visualization.DataTable(data.pieJson);

    piechart = new google.visualization.PieChart(document.getElementById('divChartPie'));
    google.visualization.events.addListener(piechart, 'select', selectHandler);
    piechart.draw(dataPie, {
        'title': 'Log Types',
        'width': 400,
        'height': 300
    });

    columnchart = new google.visualization.ScatterChart(document.getElementById('divChartColumn'));
    // google.visualization.events.addListener(columnchart, 'select', selectHandler);
    columnchart.draw(dataColumn, {
        title: 'Distrubution Of Logs By Type',
        hAxis: { title: 'TimeStamp', titleTextStyle: { color: 'red' } }
    });

    tablePie = new google.visualization.Table(document.getElementById('divTablePie'));
    google.visualization.events.addListener(tablePie, 'select', tableselectHandler);
    tablePie.draw(dataPie, {});

    tableColumn = new google.visualization.Table(document.getElementById('divTableColumn'));
    google.visualization.events.addListener(tableColumn, 'select', tableselectHandler);
    tableColumn.draw(dataColumn, {
        page: 'enable', pageSize: 25,
        allowHtml: true,
        showRowNumber: true
    });
}

function selectHandler()
{
    var selectedItem = piechart.getSelection()[0];
    var value = dataPie.getValue(selectedItem.row, 0);
    alert('The user selected ' + value);
}

function tableselectHandler()
{
    var selectedItem = table.getSelection()[0];
    var value = dataPie.getValue(selectedItem.row, 0);
    alert('The user selected ' + value);
}

var data =
{
    pieJson: {
        cols: [{ id: 'Type', label: 'Type', type: 'string' },
            { id: 'Hits', label: 'Hits', type: 'number' },
            { id: 'BestHitOn', label: 'BestHitOn', type: 'date' }
        ],
        rows: [{ c: [{ v: 'Kobi' }, { v: 1000.0 }, { v: new Date(2008, 1, 28, 0, 31, 26), f: '2/28/08 12:31 AM' }] },
            { c: [{ v: 'Pro' }, { v: 670.0 }, { v: new Date(2008, 2, 30, 0, 31, 26), f: '3/30/08 12:31 AM' }] },
            { c: [{ v: 'Plus' }, { v: 330.0 }, { v: new Date(2008, 3, 30, 0, 31, 26), f: '4/30/08 12:31 AM' }] }
        ],
        p: { foo: 'hello', bar: 'world!' }
    },

    columnJson: {
        cols: [
            { id: 'Timestamp', label: 'Timestamp', type: 'date' },
            { id: 'Kobi', label: 'Kobi', type: 'number' },
            { id: 'Pro', label: 'Pro', type: 'number' },
            { id: 'Plus', label: 'Plus', type: 'number' }
        ],
        rows: generateColumnData(),
        p: { foo: 'hello', bar: 'world!' }
    }
};

function generateColumnData()
{
    var result = [];
    for (var i = 0; i < 50; i++) {

        var d = moment().subtract('days', Math.floor((Math.random() * 30) + 1)).toDate();

        result.push(
            {
                c: [
                    {
                        v: d,
                        f : moment(d).format('DD/MM/YYYY, hh:mm:ss')
                    },
                    { v: Math.floor((Math.random() * 2000) + 1000) },
                    { v: Math.floor((Math.random() * 1000) + 500) },
                    { v: Math.floor((Math.random() * 1200) + 20) }
                ]
            });
    }

    return result;
}
