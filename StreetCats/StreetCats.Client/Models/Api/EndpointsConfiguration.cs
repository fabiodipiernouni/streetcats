
using System.Collections.Generic;

namespace StreetCats.Client.Models.Api;

/// <summary>
/// Configurazione endpoint specifici
/// </summary>
public class EndpointsConfiguration
{
    // Auth endpoints
    public string AuthLogin { get; set; } = "/auth/login";
    public string AuthRegister { get; set; } = "/auth/register";
    public string AuthRefresh { get; set; } = "/auth/refresh";
    public string AuthProfile { get; set; } = "/auth/me";

    // Cat endpoints
    public string CatsBase { get; set; } = "/cats";
    public string CatsById { get; set; } = "/cats/{id}";
    public string CatsSearch { get; set; } = "/cats/search";
    public string CatsInArea { get; set; } = "/cats/area";

    // Comment endpoints
    public string Comments { get; set; } = "/cats/{catId}/comments";
    public string CommentsById { get; set; } = "/cats/{catId}/comments/{commentId}";

    // Upload endpoints
    public string Upload { get; set; } = "/upload";
    public string UploadImages { get; set; } = "/upload/images";

    // Helper method per costruire URL completi
    public string BuildUrl(string baseUrl, string endpoint, Dictionary<string, object>? parameters = null)
    {
        var url = baseUrl.TrimEnd('/') + endpoint;

        if (parameters != null)
        {
            foreach (var param in parameters)
            {
                url = url.Replace($"{{{param.Key}}}", param.Value.ToString());
            }
        }

        return url;
    }
}