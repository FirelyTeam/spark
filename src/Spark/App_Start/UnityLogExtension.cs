using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Practices.ObjectBuilder2;
using Microsoft.Practices.Unity;
using Microsoft.Practices.Unity.ObjectBuilder;
using System.Diagnostics;

namespace Spark
{
    public class UnityLogExtension : UnityContainerExtension, IBuilderStrategy
    {
        protected override void Initialize()
        {
            Debug.WriteLine("UnityLogExtension initializing");
            Context.Strategies.Add(this, UnityBuildStage.PreCreation);
        }

        void IBuilderStrategy.PostBuildUp(IBuilderContext context)
        {
            //Type type = context.Existing == null ? context.BuildKey.Type : context.Existing.GetType();
            //Debug.WriteLine("Builded up: " + type.Name);
        }

        void IBuilderStrategy.PostTearDown(IBuilderContext context)
        {
        }

        void IBuilderStrategy.PreBuildUp(IBuilderContext context)
        {
            Type type = context.Existing == null ? context.BuildKey.Type : context.Existing.GetType();
            Debug.WriteLine("Building up: " + type.Name);
        }

        void IBuilderStrategy.PreTearDown(IBuilderContext context)
        {
        }
    }

}