using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class GradientContainer : MonoBehaviour{
	public Gradient ColorGradient; 
	public Gradient LifeGradient; 
	public Gradient DynamicLightGradient;
	public List<Gradient> EnvMultiplyGradientList = new List<Gradient>();
	public List<Gradient> EnvAddGradientList = new List<Gradient>();
}
