using Spark.Engine.Core;
using Spark.Engine.FhirResponseFactory;
using Spark.Engine.Service.FhirServiceExtensions;
using System.Threading.Tasks;

namespace Spark.Engine.Service;

public class FhirServiceR6 : FhirService
{
    public FhirServiceR6(
        IFhirModel fhirModel,
        IFhirServiceExtension[] extensions,
        IFhirResponseFactory responseFactory,
        ICompositeServiceListener serviceListener = null) : base(
        fhirModel,
        extensions,
        responseFactory,
        serviceListener
    )
    {
    }

    public override Task<FhirResponse> CapabilityStatementAsync(string sparkVersion)
    {
        var capabilityStatementService = GetFeature<ICapabilityStatementService>();
        var response = Respond.WithResource(capabilityStatementService.GetSparkCapabilityStatement());
        return Task.FromResult(response);
    }
}
