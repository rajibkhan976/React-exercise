import * as Types from "./actionTypes";
import * as Api from "../../Api/MyProfile/delegationsApi";

function setIsLoading(loading) {
    return { type: Types.SET_IS_LOADING, data: loading };
}

function setIsLoadingGroupTree(loadingGroupTree) {
    return { type: Types.SET_IS_LOADING_GROUP_TREE, data: loadingGroupTree };
}

export function loadDelegationList(sort, sortDir) {
    return function (dispatch) {
        dispatch(setIsLoading(true));
        return Api.getDelegationList(sort, sortDir)
            .then(result => {
                dispatch(setIsLoading(false));
                return dispatch({
                    type: Types.LOAD_DELEGATION_LIST_SUCCESS,
                    data: result.data
                });
            })
            .catch(error => {
                dispatch(setIsLoading(false));
                return dispatch({
                    type: Types.LOAD_DELEGATION_LIST_FAILED,
                    data: `Fetching delegation list failed ${error}`
                });
            });
    };
}

export function loadDelegationDetails(id) {
    return function (dispatch) {
        dispatch(setIsLoading(true));
        return Api.getDelegationDetails(id)
            .then(result => {
                dispatch(setIsLoading(false));
                return dispatch({
                    type: Types.LOAD_DELEGATIONS_DETAILS_SUCCESS,
                    data: result.data
                });
            })
            .catch(error => {
                dispatch(setIsLoading(false));
                return dispatch({
                    type: Types.LOAD_DELEGATIONS_DETAILS_FAILED,
                    data: `Fetching delegation details failed ${error}`
                });
            });
    };
}

export function loadGroupTree(selectedUserRoleId, destinationUserRoleId) {
    return function (dispatch) {
        dispatch(setIsLoadingGroupTree(true));
        return Api.getJsonGroupTree(selectedUserRoleId, destinationUserRoleId)
            .then(result => {
                dispatch(setIsLoadingGroupTree(false));
                return dispatch({
                    type: Types.LOAD_GROUP_TREE_SUCCESS,
                    data: result.data
                });
            })
            .catch(error => {
                dispatch(setIsLoadingGroupTree(false));
                return dispatch({
                    type: Types.LOAD_GROUP_TREE_FAILED,
                    data: `Fetching group tree failed ${error}`
                });
            });
    };
}

export function getDependsOnSelectedRolePartial(selectedUserRoleId) {
    return function (dispatch) {
        dispatch(setIsLoading(true));
        return Api.getDependsOnSelectedRolePartial(selectedUserRoleId)
            .then(result => {
                dispatch(setIsLoading(false));
                return dispatch({
                    type: Types.LOAD_SELECTED_ROLE_PARTIAL_SUCCESS,
                    data: result.data
                });
            })
            .catch(error => {
                dispatch(setIsLoading(false));
                return dispatch({
                    type: Types.LOAD_SELECTED_ROLE_PARTIAL_FAILED,
                    data: `Fetching selected role partial failed ${error}`
                });
            });
    };
}

export function loadDelegationUsers(userRoleId, searchTerm) {
    return function (dispatch) {
        dispatch(setIsLoading(true));
        return Api.getDelegationUsers(userRoleId, searchTerm)
            .then(result => {
                dispatch(setIsLoading(false));
                return dispatch({
                    type: Types.LOAD_DELEGATION_USERS_SUCCESS,
                    data: result.data
                });
            })
            .catch(error => {
                dispatch(setIsLoading(false));
                return dispatch({
                    type: Types.LOAD_DELEGATION_USERS_FAILED,
                    data: `Fetching delegation users failed ${error}`
                });
            });
    };
}

export function findDelegatedUser(userId) {
    return function (dispatch) {
        dispatch(setIsLoading(true));
        return Api.findDelegatedUser(userId)
            .then(result => {
                dispatch(setIsLoading(false));
                return dispatch({
                    type: Types.LOAD_DELEGATED_USER_SUCCESS,
                    data: result.data
                });
            })
            .catch(error => {
                dispatch(setIsLoading(false));
                return dispatch({
                    type: Types.LOAD_DELEGATED_USER_FAILED,
                    data: `Fetching delegated user failed ${error}`
                });
            })
    }
}

export function saveDelegation(
    selectedUserRoleId,
    selectedDelegationId,
    selectedUserId,
    selectedStartDate,
    selectedEndDate,
    groupCheckedIds
) {
    return function(dispatch) {
        return Api.saveDelegation(
            selectedUserRoleId,
            selectedDelegationId,
            selectedUserId,
            selectedStartDate,
            selectedEndDate,
            groupCheckedIds
        )
            .then(result => {

                if (result) {
                    dispatch(loadDelegationList("Id", "ASC"));
                }

                return dispatch({
                    type: Types.SAVE_DELEGATION_SUCCESS,
                    data: result.data
                });
            })
            .catch(error => {
                return dispatch({
                    type: Types.SAVE_DELEGATION_FAILED,
                    data: `Saving delegation failed ${error}`
                });
            });
    };
}

export function deleteDelegation(id) {
    return function(dispatch) {
        return Api.deleteDelegation(id)
            .then(result => {

                if (result) {
                    dispatch(loadDelegationList("Id", "ASC"));
                }

                return dispatch({
                    type: Types.DELETE_DELEGATION_SUCCESS,
                    data: result.data
                });
            })
            .catch(error => {
                return dispatch({
                    type: Types.DELETE_DELEGATION_FAILED,
                    data: `Deleting delegation failed ${error}`
                });
            });
    };
}