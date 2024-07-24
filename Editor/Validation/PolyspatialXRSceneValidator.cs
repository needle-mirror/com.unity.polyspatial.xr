#if HAS_XR_INTERACTION_TOOLKIT
using System;
using System.Collections.Generic;
using Unity.XR.CoreUtils.Capabilities;
using UnityEditor.PolySpatial.Validation;
using UnityEngine.XR.Interaction.Toolkit;

namespace UnityEditor.PolySpatial.XR.Validation
{
    /// <summary>
    /// Class that adds PolySpatial XR validations for components in the loaded scenes.
    /// </summary>
    static class PolySpatialXRSceneValidator
    {
        [CustomValidation]
        static void AddValidations(List<ValueTuple<Type, IComponentRuleCreator>> ruleCreators, List<ValueTuple<Type, List<ITypeMessage>>> messages)
        {
#pragma warning disable 618
            ruleCreators.Add(new(typeof(XRBaseController), new HasCapabilityRuleCreator(StandardCapabilityKeys.ControllersInput)));
#pragma warning restore 618
        }
    }
}
#endif
