import initalState from "../initialState";
import * as Types from "../../Actions/MyProfile/actionTypes";
import Update from "immutability-helper";

export default function delegationsReducer(state = initalState.myprofile, action) {
    return {
        myDelegations: myDelegationsReducer(state.myDelegations, action)
    };
}

function myDelegationsReducer(state, action) {
    switch (action.type) {

        case Types.SET_IS_LOADING:
            return { ...state, isLoading: action.data };

        case Types.SET_IS_LOADING_GROUP_TREE:
            return { ...state, isLoadingGroupTree: action.data };

        case Types.LOAD_DELEGATION_LIST_SUCCESS:
            return { ...state, list: action.data.gridModel.records };

        case Types.LOAD_DELEGATIONS_DETAILS_SUCCESS:
            return { ...state, delegationDetails: action.data };

        case Types.LOAD_DELEGABLE_ROLES_SUCCESS:
            return { ...state, delegableRoles: Update(state.delegableRoles, { $push: action.data }) };

        case Types.RESET_DELEGABLE_ROLES_SUCCESS:
            return { ...state, delegableRoles: action.data };

        case Types.LOAD_GROUP_TREE_SUCCESS:
            return { ...state, groupTree: action.data };

        case Types.LOAD_ALLOWED_GROUP_TREE_SUCCESS:
            return { ...state, groupTree: action.data };

        case Types.LOAD_SELECTED_ROLE_PARTIAL_SUCCESS:
            return { ...state, partialRole: action.data };

        case Types.LOAD_DELEGATION_USERS_SUCCESS:
            return { ...state, delegationUsers: action.data.results };

        case Types.RESET_DELEGATION_USERS_SUCCESS:
            return { ...state, delegationUsers: action.data };

        case Types.LOAD_DELEGATED_USER_SUCCESS:
            return { ...state, delegatedUser: action.data };

        case Types.LOAD_DELEGABLE_USERS_SUCCESS:
            return { ...state, delegableUsers: action.data };

        case Types.RESET_DELEGABLE_USERS_SUCCESS:
            return { ...state, delegableUsers: action.data };

        case Types.SAVE_DELEGATION_SUCCESS:
            return Update(state, { $push: [action.data] });

        case Types.DELETE_DELEGATION_SUCCESS:
            return Update(state, { $push: [action.data] });

        default:
            return state;
    }
}