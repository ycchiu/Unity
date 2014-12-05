using UnityEngine;
using EB.Sequence;
using EB.Sequence.Runtime;

[MenuItem(Path="Actions/Random")]
public class SequenceAction_Random : Action
{
    [EB.Sequence.Property]
    public int[] Probabilities = new int[0];

    private float[] _ranges = null;

    public override void Init()
    {
        base.Init();

        // normalize the probabilities;
        float sum = 0;
        for (int i = 0; i < Probabilities.Length; ++i )
        {
            sum += Probabilities[i];
        }

        float last = 0.0f;
        _ranges = new float[Probabilities.Length];
        for (int i = 0; i < Probabilities.Length; ++i)
        {
            last      += (float)Probabilities[i] / sum;
            _ranges[i] = last;
        }

        Triggers = new Trigger[Probabilities.Length];
        for (int i = 0; i < Triggers.Length; ++i)
        {
            Triggers[i] = new Trigger();
        }
    }

    [Entry]
    public void RollTheDice()
    {
        Activate();
    }

    public override bool Update()
    {
        float random = Random.value;
        for ( int i = 0; i < _ranges.Length; ++i)
        {
            if ( random <= _ranges[i] )
            {
                Triggers[i].Invoke();
                break;
            }
        }

        return false;
    }

    [Trigger(Show=false)]
    public Trigger[] Triggers = new Trigger[0];

}
