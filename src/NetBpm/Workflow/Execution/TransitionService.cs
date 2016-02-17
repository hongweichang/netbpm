﻿using NetBpm.Util.DB;
using NetBpm.Workflow.Definition;
using NetBpm.Workflow.Definition.Impl;
using NetBpm.Workflow.Delegation;
using NetBpm.Workflow.Delegation.Impl;
using NetBpm.Workflow.Execution.Impl;
using NetBpm.Workflow.Organisation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetBpm.Workflow.Execution
{
    public class TransitionService
    {
        private TransitionRepository transitionRepository = TransitionRepository.Instance;
        private AttributeRepository attributeRepository = AttributeRepository.Instance;
        private DbSession session;
        private DelegationService delegationService = null;
        private ActorExpressionResolverService actorExpressionResolverService = null;

        public TransitionService(string previousActorId,DbSession session)
        {
            // TODO: Complete member initialization
            this.session = session;
            delegationService = new DelegationService();
            actorExpressionResolverService = new ActorExpressionResolverService(previousActorId);
        }

        public TransitionImpl GetTransition(string transitionName, IState state, DbSession dbSession)
        {
            return transitionRepository.GetTransition(transitionName, (StateImpl)state, dbSession);
        }

        public void ProcessTransition(TransitionImpl transition, FlowImpl flow, DbSession dbSession)
        {
            NodeImpl destination = (NodeImpl)transition.To;
            flow.Node = destination;

            if (destination is ActivityStateImpl)
            {
                ProcessActivityState((ActivityStateImpl)destination, flow, dbSession);
            }
            else if (destination is ProcessStateImpl)
            {
                //ProcessProcessState((ProcessStateImpl)destination, executionContext, dbSession);
            }
            else if (destination is DecisionImpl)
            {
                ProcessDecision((DecisionImpl)destination, flow, dbSession);
            }
            else if (destination is ForkImpl)
            {
                //ProcessFork((ForkImpl)destination, executionContext, dbSession);
            }
            else if (destination is JoinImpl)
            {
                //ProcessJoin((JoinImpl)destination, executionContext, dbSession);
            }
            else if (destination is EndStateImpl)
            {
                ProcessEndState((EndStateImpl)destination, flow,dbSession);
            }
            else
            {
                throw new SystemException("");
            }
        }

        public void ProcessActivityState(ActivityStateImpl activityState, FlowImpl flow, DbSession dbSession)
        {
            String actorId = null;
            String role = activityState.ActorRoleName;
          
            DelegationImpl assignmentDelegation = activityState.AssignmentDelegation;

            if (assignmentDelegation != null)
            {
                var delegateParameters = delegationService.ParseConfiguration(activityState.AssignmentDelegation);
                actorExpressionResolverService.CurrentScope = flow;
                actorExpressionResolverService.DbSession = dbSession;
                actorId = actorExpressionResolverService.ResolveArgument(delegateParameters["expression"].ToString()).Id;

                if ((Object)actorId == null)
                {
                    throw new SystemException("invalid process definition : assigner of activity-state '" + activityState.Name + "' returned null instead of a valid actorId");
                }
            }
            else
            {
                if ((Object)role != null)
                {
                    IActor actor = null;
                    var attr = attributeRepository.FindAttributeInstanceInScope(role, flow, dbSession);
                    if (attr != null)
                        actor = (IActor)attr.GetValue();

                    if (actor == null)
                    {
                        throw new SystemException("invalid process definition : activity-state must be assigned to role '" + role + "' but that attribute instance is null");
                    }
                    actorId = actor.Id;
                }
                else
                {
                    throw new SystemException("invalid process definition : activity-state '" + activityState.Name + "' does not have an assigner or a role");
                }
            }

            flow.ActorId = actorId;

            // If necessary, store the actor in the role
            if ((string.IsNullOrEmpty(role) == false) && (assignmentDelegation != null))
            {
                //executionContext.StoreRole(actorId, activityState);
            }

            // the client of performActivity wants to be Informed of the people in charge of the process
            //executionContext.AssignedFlows.Add(flow);
        }

        private void ProcessEndState(EndStateImpl endState, FlowImpl flow,DbSession dbSession)
        {
            flow.ActorId = null;
            flow.End = DateTime.Now;
            flow.Node = endState; 
        }

        public void ProcessDecision(DecisionImpl decision, FlowImpl flow, DbSession dbSession)
        {
            //var delegateParameters = delegationService.ParseConfiguration(decision.DecisionDelegation);
            
            //// delegate the decision 
            //TransitionImpl selectedTransition = delegationHelper.DelegateDecision(decision.DecisionDelegation, executionContext);

            //// process the selected transition
            //ProcessTransition(selectedTransition, flow, dbSession);
        }
    }
}
