using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Hosting;

namespace Plugin.Maui.GeoLocator;

/// <summary>
/// Registers the GeoLocator plugin with the MAUI dependency injection container.
/// </summary>
public static class MauiAppBuilderExtensions
{
	/// <summary>
	/// Adds <see cref="IGeoLocator"/> as a singleton and optionally enables logging.
	/// </summary>
	/// <example>
	/// <code>
	/// builder.UseGeoLocator(options =>
	/// {
	///     options.EnableLogging = true;
	/// });
	/// </code>
	/// </example>
	public static MauiAppBuilder UseGeoLocator(this MauiAppBuilder builder, Action<GeoLocatorOptions>? configure = null)
	{
		ArgumentNullException.ThrowIfNull(builder);

		var options = new GeoLocatorOptions();
		configure?.Invoke(options);

		builder.Services.AddSingleton(options);
		builder.Services.AddSingleton<IGeoLocator>(serviceProvider =>
		{
			var locator = GeoLocator.Current;
			var resolvedOptions = serviceProvider.GetRequiredService<GeoLocatorOptions>();

			if (resolvedOptions.EnableLogging)
			{
				IGeoLocatorLogger logger = resolvedOptions.Logger
					?? CreateLoggerAdapter(serviceProvider)
					?? new DebugGeoLocatorLogger();

				locator.EnableLogging(true, logger);
			}

			return locator;
		});

		return builder;
	}

	static IGeoLocatorLogger? CreateLoggerAdapter(IServiceProvider serviceProvider)
	{
		var factory = serviceProvider.GetService<ILoggerFactory>();
		return factory is null ? null : new MicrosoftLoggerAdapter(factory.CreateLogger("Plugin.Maui.GeoLocator"));
	}
}
