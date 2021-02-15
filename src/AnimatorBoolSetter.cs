using UnityEngine;

namespace GameSystem.AnimatorTools
{
    public class AnimatorBoolSetter : StateMachineBehaviour
    {
        public string parameter;
        public bool value;

        private int _parameterHash;

        private bool _isInit;
        
        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (!_isInit)
            {
                _parameterHash = Animator.StringToHash(parameter);
                _isInit = true;
            }
            
            animator.SetBool(_parameterHash, value);
        }
    }
}
