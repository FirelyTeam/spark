using System;
using System.Web.Http.Controllers;
using System.Web.Http.Metadata;
using System.Web.Http.Validation;

namespace Spark
{
    public class TypePrefixBodyModelValidator : IBodyModelValidator
    {
        private readonly IBodyModelValidator innerValidator;

        public TypePrefixBodyModelValidator(IBodyModelValidator innerValidator)
        {
            if (innerValidator == null)
            {
                throw new ArgumentNullException("innerValidator");
            }

            this.innerValidator = innerValidator;
        }

        public bool Validate(object model, Type type, ModelMetadataProvider metadataProvider, HttpActionContext actionContext, string keyPrefix)
        {
            // Remove the keyPrefix but otherwise let innerValidator do what it normally does.
            return innerValidator.Validate(model, type, metadataProvider, actionContext, "bla ba");
        }
    }
}