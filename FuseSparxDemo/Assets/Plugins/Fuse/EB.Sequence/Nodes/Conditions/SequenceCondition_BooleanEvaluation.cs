using EB.Sequence;
using EB.Sequence.Runtime;
using UnityEngine;

[MenuItem(Path="Conditions/Boolean Evaluation")]
public class SequenceCondition_BooleanEvaluation : Condition
{
    [EB.Sequence.Property]
    public int Inputs = 0;

    [EB.Sequence.Property]
    public string[] Evaluations = new string[0];

    [Variable(ExpectedType = typeof(float), Show=false)]
    public Variable[] Input = new Variable[0];

    public override void  Init()
    {
 	    base.Init();

        Triggers = new Trigger[Input.Length];
        for ( int i = 0; i < Triggers.Length; ++i )
        {
            Triggers[i] = new Trigger();
        }
    }

    public void EvaluationExpression(string eval, Trigger trigger)
    {
        if (eval.Length == 0)
            return;

        string s = eval;

        for ( int i = 0; i < Input.Length; ++i )
        {
            Variable var = Input[i] ?? Variable.Null;
            object obj = var.Value;
            if ( obj != null )
            {
                s = s.Replace( kAlphabet.Substring(i,1), obj.ToString() );
            }
        }

        bool bResult = EB.Sequence.BooleanEvaluator.Evaluate(s);

        EB.Debug.Log("SequenceCondition_BooleanEvaluation Result:" + bResult);
        EB.Debug.Log("SequenceCondition_BooleanEvaluation Evaluation:" + s);

        if (bResult)
        {
            if (trigger != null)
            {
                trigger.Invoke();
            }
        }
    }

    public override bool Update()
    {
        for (int i = 0; i < Evaluations.Length; ++i )
        {
            EvaluationExpression(Evaluations[i], Triggers[i]);
        }
        return false;
    }

    [Trigger(Show=false)]
    public Trigger[] Triggers = new Trigger[0];

}


