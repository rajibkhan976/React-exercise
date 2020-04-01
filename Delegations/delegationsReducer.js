import initalState from "../initialState";
import * as Types from "../../Actions/MyProfile/actionTypes";
import Update from "immutability-helper";

export default function delegationsReducer(state = initalState.delegations, action) {
    return {
        myDelegations: myDelegationsReducer(state.myDelegations, action)
    };
}

function myDelegationsReducer(state, action) {
    switch (action.type) {

        case Types.LOAD_DELEGATION_LIST_SUCCESS:
            return { ...state, list: action.data.gridModel.records };

        case Types.LOAD_DELEGATIONS_DETAILS_SUCCESS:
            return { ...state, delegationDetails: action.data };

        case Types.LOAD_GROUP_TREE_SUCCESS:
            return { ...state, groupTree: action.data };

        case Types.LOAD_SELECTED_ROLE_PARTIAL_SUCCESS:
            return { ...state, partialRole: action.data };

        case Types.LOAD_DELEGATION_USERS_SUCCESS:
            return { ...state, delegationUsers: action.data.results };

        case Types.SAVE_DELEGATION_SUCCESS:
            return Update(state, { $push: [action.data] });

        case Types.DELETE_DELEGATION_SUCCESS:
            return Update(state, { $push: [action.data] });

        default:
            return state;
    }
}