using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Snail.MCP.Playwright.Adapters;
using Snail.MCP.Playwright.Application;
using Snail.MCP.Playwright.Configuration;
using Snail.Toolkit.Web.Scraping.Agent.Extensions;
using Snail.Toolkit.Web.Scraping.Extensions;
using Snail.Toolkit.Web.Search;
using Snail.Toolkit.Web.Search.Browser.Agent.Extensions;
using Snail.Toolkit.Web.Search.Browser.Extensions;
using Snail.Toolkit.Web.Search.Extensions;
using Snail.Toolkit.Web.Search.Providers;

namespace Snail.MCP.Playwright.Extensions;

/// <summary>Driven side: the toolkit that searches and reads, wired the way this deployment asked.</summary>
public static class BrowsingRegistrationExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Everything the tools reach the web through: the page reader, the engines and the health of both.</summary>
        public IServiceCollection AddBrowsing(ServerConfig config, IConfiguration settings)
        {
            services.AddSearchEngines(SearchEngines.Named(config.Engines), settings);
            services.AddPageReading(settings);
            services.TryAddSingleton<IBrowserServer, HttpBrowserServer>();
            services.TryAddSingleton<Health>();

            return services;
        }

        /// <summary>Reading a page: the rules an address has to pass, the reader and the memory of what was read.</summary>
        private IServiceCollection AddPageReading(IConfiguration settings)
        {
            services.AddScraping(settings);
            services.TryAddSingleton<AddressRules>();
            services.TryAddSingleton<PageMemory>();
            services.TryAddSingleton<PageReading>();
            services.TryAddSingleton<BatchReading>();
            services.TryAddSingleton<SiteReading>();

            return services.AddCloserReading(settings);
        }

        /// <summary>What plain reading does not give: pieces by expression, a picture, and a page after a form was filled in.</summary>
        private IServiceCollection AddCloserReading(IConfiguration settings)
        {
            services.AddPageAgent(settings);
            services.TryAddSingleton<IMarkupSelection, MarkupSelection>();
            services.TryAddSingleton<PageExtraction>();
            services.TryAddSingleton<PageFields>();
            services.TryAddSingleton<PagePictures>();
            services.TryAddSingleton<PageWalking>();

            return services;
        }

        /// <summary>The engines the deployment named, and no others.</summary>
        /// <remarks>
        /// The toolkit registers DuckDuckGo with the search itself, because it is the one engine that needs
        /// nothing running. A deployment that leaves it out has a reason — an exit DuckDuckGo refuses, for
        /// one — so its registration is taken back rather than left to answer with refusals.
        /// </remarks>
        private IServiceCollection AddSearchEngines(IReadOnlyList<string> engines, IConfiguration settings)
        {
            services.AddSearch(settings);

            if (engines.Contains(SearchEngines.DuckDuckGoBrowser))
                services.AddDuckDuckGo(settings);

            if (engines.Contains(SearchEngines.Bing))
                services.AddBing(settings);

            if (engines.Contains(SearchEngines.Brave))
                services.AddBrave(settings);

            if (engines.Contains(SearchEngines.Google))
                services.AddGoogle(settings);

            if (!engines.Contains(SearchEngines.DuckDuckGo))
                services.Without<DuckDuckGo>();

            services.TryAddSingleton<LinkSearch>();

            return services;
        }

        private void Without<TProvider>()
            where TProvider : class, ISearchProvider
        {
            var registered = services
                .Where(registration => registration.ServiceType == typeof(ISearchProvider) && registration.ImplementationType == typeof(TProvider))
                .ToList();

            foreach (var registration in registered)
                services.Remove(registration);
        }
    }
}
