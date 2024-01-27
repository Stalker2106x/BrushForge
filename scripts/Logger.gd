extends Node

var logNode;

func info(message):
    var color = "aqua";
    logNode.append_text("[color=%s]> %s[/color]\n" % [color, message]);

func error(message):
    var color = "red";
    logNode.append_text("[color=%s]> %s[/color]\n" % [color, message]);

func success(message):
    var color = "green";
    logNode.append_text("[color=%s]> %s[/color]\n" % [color, message]);
