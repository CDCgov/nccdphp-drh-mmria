using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using mmria.common.metadata;

namespace mmria_case_generator.Generators
{
    public class MetadataManager
    {
        public app? Metadata { get; private set; }
        public Dictionary<string, value_node[]> Lookup { get; private set; } = new();
        public Dictionary<string, MetadataNode> NodeDictionary { get; private set; } = new();

        public async Task<app> FetchMetadataAsync(string metadataUrl)
        {
            Console.WriteLine($"Fetching metadata from: {metadataUrl}");
            
            using var httpClient = new HttpClient();
            var metadata = await httpClient.GetFromJsonAsync<app>(metadataUrl)
                ?? throw new Exception("Failed to fetch metadata");

            Metadata = metadata;
            BuildLookupDictionary(metadata);
            ParseMetadataNodes(metadata);

            Console.WriteLine($"✓ Metadata fetched successfully");
            return metadata;
        }

        private void BuildLookupDictionary(app metadata)
        {
            if (metadata.lookup == null) return;

            foreach (var lookupNode in metadata.lookup)
            {
                if (lookupNode.name != null && lookupNode.values != null)
                {
                    Lookup[lookupNode.name] = lookupNode.values;
                }
            }
        }

        private void ParseMetadataNodes(app metadata)
        {
            if (metadata.children == null) return;

            foreach (var child in metadata.children)
            {
                if (child.type?.ToLowerInvariant() == "form")
                {
                    bool isMultiform = child.cardinality == "*" || child.cardinality == "+";
                    ParseMetadataNode(child, "", isMultiform: isMultiform);
                }
            }
        }

        private void ParseMetadataNode(node currentNode, string parentPath, bool isMultiform = false, bool inGrid = false)
        {
            var nodePath = string.IsNullOrEmpty(parentPath) 
                ? currentNode.name 
                : $"{parentPath}/{currentNode.name}";

            // Normalize type to lowercase to handle metadata inconsistencies (e.g., "List" vs "list")
            var nodeType = currentNode.type?.ToLowerInvariant() ?? "";

            // Handle path_reference (lookup references)
            if (nodeType == "list" && !string.IsNullOrEmpty(currentNode.path_reference))
            {
                var metadataNode = new MetadataNode
                {
                    Path = nodePath,
                    Node = currentNode,
                    IsMultiform = isMultiform,
                    IsGrid = inGrid,
                    IsMultiSelect = currentNode.is_multiselect == true,
                    ListItemDataType = currentNode.list_item_data_type
                };

                // Extract lookup name from path_reference (e.g., "lookup/state" → "state")
                var lookupName = currentNode.path_reference.Replace("lookup/", "");
                if (Lookup.TryGetValue(lookupName, out var values))
                {
                    foreach (var val in values)
                    {
                        // Exclude special placeholder values (9999=unspecified, 7777=not applicable, 8888=unknown)
                        if (val.value != null && val.display != null && 
                            val.value != "9999" && val.value != "7777" && val.value != "8888")
                        {
                            metadataNode.ValueToDisplay[val.value] = val.display;
                            metadataNode.DisplayToValue[val.display] = val.value;
                        }
                    }
                }

                NodeDictionary[nodePath] = metadataNode;
            }
            // Handle inline values
            else if (nodeType == "list" && currentNode.values != null)
            {
                var metadataNode = new MetadataNode
                {
                    Path = nodePath,
                    Node = currentNode,
                    IsMultiform = isMultiform,
                    IsGrid = inGrid,
                    IsMultiSelect = currentNode.is_multiselect == true,
                    ListItemDataType = currentNode.list_item_data_type
                };

                foreach (var val in currentNode.values)
                {
                    // Exclude special placeholder values (9999=unspecified, 7777=not applicable, 8888=unknown)
                    if (val.value != null && val.display != null && 
                        val.value != "9999" && val.value != "7777" && val.value != "8888")
                    {
                        metadataNode.ValueToDisplay[val.value] = val.display;
                        metadataNode.DisplayToValue[val.display] = val.value;
                    }
                }

                NodeDictionary[nodePath] = metadataNode;
            }
            // Handle grids
            else if (nodeType == "grid")
            {
                var metadataNode = new MetadataNode
                {
                    Path = nodePath,
                    Node = currentNode,
                    IsMultiform = isMultiform,
                    IsGrid = true
                };
                NodeDictionary[nodePath] = metadataNode;

                // Process grid children
                if (currentNode.children != null)
                {
                    foreach (var child in currentNode.children)
                    {
                        ParseMetadataNode(child, nodePath, isMultiform, inGrid: true);
                    }
                }
            }
            // Handle groups and other containers
            else if (nodeType == "group" || nodeType == "form")
            {
                var metadataNode = new MetadataNode
                {
                    Path = nodePath,
                    Node = currentNode,
                    IsMultiform = isMultiform,
                    IsGrid = inGrid
                };
                NodeDictionary[nodePath] = metadataNode;

                // Recursively process children
                if (currentNode.children != null)
                {
                    foreach (var child in currentNode.children)
                    {
                        ParseMetadataNode(child, nodePath, isMultiform, inGrid);
                    }
                }
            }
            // Handle simple fields
            else
            {
                var metadataNode = new MetadataNode
                {
                    Path = nodePath,
                    Node = currentNode,
                    IsMultiform = isMultiform,
                    IsGrid = inGrid
                };
                NodeDictionary[nodePath] = metadataNode;
            }
        }

        public List<MetadataNode> GetForms()
        {
            return NodeDictionary.Values
                .Where(n => n.Node.type?.ToLowerInvariant() == "form")
                .ToList();
        }

        public List<MetadataNode> GetGrids()
        {
            return NodeDictionary.Values
                .Where(n => n.Node.type?.ToLowerInvariant() == "grid")
                .ToList();
        }

        public List<MetadataNode> GetNodesByType(string type)
        {
            var normalizedType = type?.ToLowerInvariant();
            return NodeDictionary.Values
                .Where(n => n.Node.type?.ToLowerInvariant() == normalizedType)
                .ToList();
        }
    }

    public class MetadataNode
    {
        public bool IsMultiform { get; set; }
        public bool IsGrid { get; set; }
        public bool IsMultiSelect { get; set; }
        public string Path { get; set; } = "";
        public node Node { get; set; } = new node();
        public Dictionary<string, string> ValueToDisplay { get; set; } = new();
        public Dictionary<string, string> DisplayToValue { get; set; } = new();
        public string? ListItemDataType { get; set; }
    }
}


