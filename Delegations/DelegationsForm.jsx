import React from "react";
import { connect } from "react-redux";
import PropTypes from "prop-types";
import { bindActionCreators } from "redux";
import * as delegationsActions from "../../../Actions/MyProfile/delegationsActions";
import translate from "redux-polyglot/translate";
import DatePicker from "react-datepicker";
import "react-datepicker/dist/react-datepicker.css";
import Select from "react-select";

const DelegationsForm = ({
    p,
    isLoading,
    editIndex,
    delegationDetails,
    delegateRole,
    delegationUsers,
    userName,
    startingDate,
    endingDate,
    handleChangeDelegateRole,
    handleChangeUserName,
    handleChangeStartingDate,
    handleChangeEndingDate,
    assignDelegation,
    updateDelegation,
    handleModalClose,
    delegationsActions
}) => {

    const setRolesOptions = (delegationDetails) => {
        let options  = [];
        if (!isLoading &&
            delegationDetails !== undefined &&
            delegationDetails.roles !== undefined &&
            delegationDetails.roles.length !== 0) {
            delegationDetails.roles.map((value, index) => {
                if (value.startDate !== null && value.endDate === null) {
                    options.push(
                        { value: `${value.role.name}`, label: `${value.role.name} (${value.startDate} - )` }
                    );
                } else if (value.startDate === null && value.endDate !== null) {
                    options.push(
                        { value: `${value.role.name}`, label: `${value.role.name} ( - ${value.endDate})` }
                    );
                } else if (value.startDate !== null && value.endDate !== null) {
                    options.push(
                        { value: `${value.role.name}`, label: `${value.role.name} (${value.startDate} - ${value.endDate})` }
                    );
                } else {
                    options.push(
                        { value: `${value.role.name}`, label: `${value.role.name} ( - )` }
                    );
                }
            })
        }
        return options;
    }

    const setDelegableUsersOptions = (delegationUsers) => {
        let options = [];
        if (!isLoading &&
            delegationUsers !== undefined &&
            delegationUsers.length !== 0) {
            delegationUsers.map((value, index) => {
                options.push(
                    { value: `${value.text}`, label: `${value.text}` }
                );
            })
        }
        return options;
    }

    return (
        <div className="delegations-form">
            <p className="delegations-form-label">{p.t("delegations_list_label_roll")}</p>
            <Select
                className="select-delegations-rolls"
                name="form-field-name"
                value={delegateRole}
                onChange={(delegateRole) => handleChangeDelegateRole(delegateRole, delegationDetails)}
                options={setRolesOptions(delegationDetails)}
                disabled={editIndex ? true : false}
                isLoading={isLoading ? true : false}
            />
            <p className="delegations-form-label">{p.t("delegations_list_label_user")}</p>
            <Select
                className="select-delegations-user"
                name="form-field-name"
                value={userName}
                onChange={(userName) => handleChangeUserName(userName)}
                options={setDelegableUsersOptions(delegationUsers)}
                disabled={editIndex ? true : false}
                isLoading={isLoading ? true : false}
            />
            <p className="delegations-form-label">{p.t("delegations_list_header_starting_from")}</p>
            <DatePicker
                className="delegations-period"
                selected={startingDate}
                onChange={handleChangeStartingDate}
            />
            <p className="delegations-form-label">{p.t("delegations_list_header_ending_by")}</p>
            <DatePicker
                className="delegations-period"
                selected={endingDate}
                onChange={handleChangeEndingDate}
            />
        </div>
    );
}

DelegationsForm.propTypes = {
    p: PropTypes.object,
    isLoading: PropTypes.bool,
    list: PropTypes.array,
    delegationDetails: PropTypes.object,
    groupTree: PropTypes.array,
    partialRole: PropTypes.object,
    delegationUsers: PropTypes.array
};

function mapStateToProps(state) {
    return {
        isLoading: state.delegations.myDelegations.isLoading,
        list: state.delegations.myDelegations.list,
        delegationDetails: state.delegations.myDelegations.delegationDetails,
        groupTree: state.delegations.myDelegations.groupTree,
        partialRole: state.delegations.myDelegations.partialRole,
        delegationUsers: state.delegations.myDelegations.delegationUsers
    };
}

function mapDispatchToProps(dispatch) {
    return {
        delegationsActions: bindActionCreators(delegationsActions, dispatch)
    };
}

export default connect(mapStateToProps, mapDispatchToProps)(translate(DelegationsForm));