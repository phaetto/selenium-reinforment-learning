﻿namespace Selenium.Algorithms.ReinforcementLearning
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Uses the policy's data to find routes to goals
    /// </summary>
    /// <typeparam name="TData">The prime data for state - exposed for convienience</typeparam>
    public sealed class RLPathFinder<TData> : IRLPathFinder<TData>
    {
        private readonly IEnvironment<TData> environment;
        private readonly IPolicy<TData> policy;

        public RLPathFinder(
            in IEnvironment<TData> environment,
            in IPolicy<TData> policy)
        {
            this.environment = environment;
            this.policy = policy;
        }

        /// <summary>
        /// Gets the best route from the starting state to the goal state making decisions using the maximum reward path.
        /// Mutates the environment.
        /// </summary>
        /// <param name="start">The starting state</param>
        /// <param name="target">The target state</param>
        /// <param name="maxSteps">Maximum steps that should be taken</param>
        /// <returns>A report data structure that describes what happened while attempting</returns>
        public async Task<WalkResult<TData>> FindRoute(IState<TData> start, ITrainGoal<TData> trainGoal, int maxSteps = 10)
        {
            if (trainGoal.TimesReachedGoal == 0)
            {
                return new WalkResult<TData>(PathFindResultState.GoalNeverReached);
            }

            var resultStates = new List<StateAndActionPair<TData>>();

            var currentState = start;
            var currentStep = 0;
            while (currentStep < maxSteps)
            {
                var actions = await environment.GetPossibleActions(currentState);
                var stateAndActionPairs = actions
                    .Select(x =>
                    {
                        var pair = new StateAndActionPair<TData>(currentState, x);
                        return policy.QualityMatrix.ContainsKey(pair)
                            ? (action: x, score: policy.QualityMatrix[pair])
                            : (action: x, score: 0D);
                    })
                    .ToList();

                if (stateAndActionPairs.Count < 1)
                {
                    return new WalkResult<TData>(PathFindResultState.Unreachable);
                }

                var maximumValue = 0D;
                var maximumReturnAction = stateAndActionPairs.First().action;
                foreach (var pair in stateAndActionPairs)
                {
                    if (pair.score > maximumValue)
                    {
                        maximumReturnAction = pair.action;
                        maximumValue = pair.score;
                    }
                }

                var newState = await maximumReturnAction.ExecuteAction(environment, currentState);

                while (await environment.IsIntermediateState(newState) && currentStep < maxSteps)
                {
                    await environment.WaitForPostActionIntermediateStabilization();
                    newState = await environment.GetCurrentState();
                    ++currentStep;
                }

                if (currentStep >= maxSteps)
                {
                    return new WalkResult<TData>(PathFindResultState.StepsExhausted, resultStates);
                }

                var newPair = new StateAndActionPairWithResultState<TData>(currentState, maximumReturnAction, newState);

                if (resultStates.Contains(newPair))
                {
                    return new WalkResult<TData>(PathFindResultState.LoopDetected, resultStates);
                }

                resultStates.Add(newPair);
                currentState = newState;

                if (await trainGoal.HasReachedAGoalCondition(currentState, maximumReturnAction))
                {
                    return new WalkResult<TData>(PathFindResultState.GoalReached, resultStates);
                }

                ++currentStep;
            }

            return new WalkResult<TData>(PathFindResultState.StepsExhausted, resultStates);
        }

        public async Task<WalkResult<TData>> FindRouteWithoutApplyingActions(IState<TData> start, ITrainGoal<TData> trainGoal, int maxSteps = 10)
        {
            if (trainGoal.TimesReachedGoal == 0)
            {
                return new WalkResult<TData>(PathFindResultState.GoalNeverReached);
            }

            var resultStates = new List<StateAndActionPair<TData>>();

            var currentState = start;
            var currentStep = 0;
            while (currentStep < maxSteps)
            {
                var actions = await environment.GetPossibleActions(currentState);
                var stateAndActionPairs = actions
                   .Select(x =>
                   {
                       var pair = new StateAndActionPair<TData>(currentState, x);
                       return policy.QualityMatrix.ContainsKey(pair)
                           ? (pair, score: policy.QualityMatrix[pair])
                           : (pair, score: 0D);
                   })
                   .ToList();

                if (stateAndActionPairs.Count < 1)
                {
                    return new WalkResult<TData>(PathFindResultState.Unreachable);
                }

                var maximumValue = 0D;
                var maximumReturnPair = stateAndActionPairs.First().pair;
                foreach (var pairAndScore in stateAndActionPairs)
                {
                    if (pairAndScore.score > maximumValue)
                    {
                        maximumReturnPair = pairAndScore.pair;
                        maximumValue = pairAndScore.score;
                    }
                }

                if (maximumReturnPair is StateAndActionPairWithResultState<TData> stateAndActionPairWithResultState)
                {
                    var newState = stateAndActionPairWithResultState.ResultState;

                    if (resultStates.Contains(stateAndActionPairWithResultState))
                    {
                        return new WalkResult<TData>(PathFindResultState.LoopDetected, resultStates);
                    }

                    resultStates.Add(stateAndActionPairWithResultState);
                    currentState = newState;

                    if (await trainGoal.HasReachedAGoalCondition(currentState, stateAndActionPairWithResultState.Action))
                    {
                        return new WalkResult<TData>(PathFindResultState.GoalReached, resultStates);
                    }
                }
                else
                {
                    return new WalkResult<TData>(PathFindResultState.DataNotIncluded);
                }

                ++currentStep;
            }

            return new WalkResult<TData>(PathFindResultState.StepsExhausted, resultStates);
        }
    }
}
