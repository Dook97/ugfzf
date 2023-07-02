﻿using System.Text.Json.Nodes;

namespace JsonTools;
public static class JsonTools
{
    public static JsonNode? GetByPath(this JsonNode node, string path)
    {
        JsonNode? output = node;
        var pathArr = path.Split(".");
        for (int i = 0; i < pathArr.Length; ++i) {
            if (output is null)
                return null;
            output = output[pathArr[i]];
        }
        return output;
    }
}