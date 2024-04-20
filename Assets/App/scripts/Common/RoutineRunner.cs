using System;
using strange.extensions.context.api;
using UnityEngine;
using System.Collections;
using strange.extensions.injector.api;

//An implicit binding. We map this binding as Cross-Context by default.
[Implements(typeof(IRoutineRunner), InjectionBindingScope.CROSS_CONTEXT)]
public class RoutineRunner : IRoutineRunner
{
	[Inject(ContextKeys.CONTEXT_VIEW)]
	public GameObject contextView{ get; set; }

	private RoutineRunnerBehaviour mb;

	[PostConstruct]
	public void PostConstruct()
	{
		mb = contextView.AddComponent<RoutineRunnerBehaviour> ();
	}

	public Coroutine StartCoroutine(IEnumerator routine)
	{
		return mb.StartCoroutine(routine);
	}

	public void StopCoroutine(IEnumerator routine)
	{
		mb.StopCoroutine(routine);
	}
}

public class RoutineRunnerBehaviour : MonoBehaviour
{
	
}

