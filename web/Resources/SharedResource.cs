namespace TechSpecs;

// Marker class for IStringLocalizer<SharedResource> / IHtmlLocalizer<SharedResource>.
// Must be in root namespace: ResourcesPath="Resources" + stripped type name = "SharedResource"
// → resolves to Resources/SharedResource.resx (not Resources/Resources/SharedResource.resx).
public class SharedResource { }
