// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/ProHub").build();

connection.on("ReceiveMessage", function (message) {
    var msg = message.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
    var encodedMsg = " Current search reqest " + msg;
    
    document.getElementById("placeholder").innerHTML = encodedMsg;
});

connection.on("ReceiveCurrentResult", function (result) {
    var headers = ["id", "brand", "originalPrice", "firstPrice", "secondPrice", "isSuccessful"];
    var searchResultsTable = document.getElementById("searchResultsTable");
    if (searchResultsTable == null) {
        var searchResultsTable = document.createElement('table');
        searchResultsTable.setAttribute('contenteditable', 'true');
        var div = document.getElementById('searachResults');
        searchResultsTable.id = "searchResultsTable";
        var tr = searchResultsTable.insertRow(-1);
        for (var i = 0; i < headers.length; i++) {
            var th = document.createElement('th');
            th.innerHTML = headers[i];
            tr.appendChild(th);
        };
        div.innerHTML = "";
        div.appendChild(searchResultsTable);
    }
    var matchFound = false;
    if (searchResultsTable.firstElementChild.childElementCount > 1) {
        var tr = document.getElementById(result.id + result.brand);
        if (tr != null) {
            var cells = tr.cells;
            if (cells != null && tr.id == result.id + result.brand) {
                cells[3].innerHTML = result.firstPrice;
                cells[4].innerHTML = result.secondPrice;
                cells[5].innerHTML = result.isSuccessful;
                matchFound = true;
            }
        }
    }

    if (!matchFound) {
        var tr = searchResultsTable.insertRow(-1);
        tr.setAttribute('id', result.id + result.brand);
        for (var j = 0; j < headers.length; j++) {
            var tabCell = tr.insertCell(-1);
            tabCell.innerHTML = result[headers[j]];
        };
    }
});

connection.on("ProgressChanged", function (progress) {
    if (typeof (progress) != "number")
        return;
    if (progress > 100 || progress < 0)
        return;
    var markup = progress.toString();
    if (progress < 100) {
        markup = progress.toString();
    }
    document.getElementById("progressBar").setAttribute('style', 'width:' + markup + '%');
    document.getElementById("progressBar").setAttribute('aria-valuenow', markup);
    document.getElementById("progressBar").textContent = markup + ' %';
});



connection.start().catch(function (err) {
    return console.error(err.toString());
});
