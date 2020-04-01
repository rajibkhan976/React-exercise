import React, { useState, useEffect, useRef } from "react";
import { connect } from "react-redux";
import PropTypes from "prop-types";
import { bindActionCreators } from "redux";
import moment from "moment";
import * as delegationsActions from "../../../Actions/MyProfile/delegationsActions";
import translate from "redux-polyglot/translate";
import * as styles from "react-virtualized/styles.css";
import { ColumnSizer, AutoSizer, MultiGrid } from "react-virtualized";
import { replaceNodeFromTree } from "react-virtualized-tree/lib/selectors/nodes";
import { getFlattenedTree } from "react-virtualized-tree/lib/selectors/getFlattenedTree";
import { FadeUp } from "../../../Utils/transitionWrappers";
import DelegationsModal from "./DelegationsModal";

const DelegationsList = ({ p, list, delegationDetails, groupTree, partialRole, delegationUsers, delegationsActions }) => {

    const [delegateRole, setDelegateRole] = useState('');
    const [nodes, setNodes] = useState(undefined);
    const [userName, setUserName] = useState('');
    const [startingDate, setStartingDate] = useState(undefined);
    const [endingDate, setEndingDate] = useState(undefined);
    const [toggleModal, setToggleModal] = useState(false);
    const initialDelegations = [
        [   p.t("delegations_list_header_manage_delegation"),
            p.t("delegations_list_header_role"),
            p.t("delegations_list_header_user"),
            p.t("delegations_list_header_start_from"),
            p.t("delegations_list_header_end_by")
        ]
    ];

    const [delegations, setDelegations] = useState(initialDelegations);
    const [delegationsIds, setDelegationsIds] = useState([]);

    useEffect(() => {
        let delegationsArray = [
            [   p.t("delegations_list_header_manage_delegation"),
                p.t("delegations_list_header_role"),
                p.t("delegations_list_header_user"),
                p.t("delegations_list_header_start_from"),
                p.t("delegations_list_header_end_by")
            ]
        ];
        let delegationsIdArray = [[""]];
        if (list !== undefined && list.length !== 0) {
            list.map((value, index) => {
                delegationsArray.push(['',
                `${value.destinationUserRole.role.name}`,
                `${value.destinationUserRole.user.fullName}`,
                `${moment(value.destinationUserRole.startDate).format('YYYY-MM-DD')}`,
                `${moment(value.destinationUserRole.endDate).format('YYYY-MM-DD')}`]);

                delegationsIdArray.push(value.id);
            });
        }
        setDelegations(delegationsArray);
        setDelegationsIds(delegationsIdArray);
    }, [list]);

    const [selectedUserId, setSelectedUserId] = useState(undefined);
    const [editDelegatedUser, setEditDelegatedUser] = useState('');
    const [editIndex, setEditIndex] = useState(null);
    const [deleteIndex, setDeleteIndex] = useState(null);
    const [rowIndex, setRowIndex] = useState(null);
    const [rowHeight, setRowHeight] = useState(40);
    const [gridRef, setGridRef] = useState(undefined);
    const [validationError, setValidationError] = useState(undefined);

    const handleModalClose = () => {
        if (deleteIndex !== null) {
            setDeleteIndex(null);
        }
        if (editIndex !== null) {
            setEditIndex(null);
        }
        if (rowIndex !== null) {
            setRowIndex(null);
        }
        if (editDelegatedUser !== '') {
            setEditDelegatedUser('');
        }
        if (userName !== '') {
            setUserName('');
        }
        if (validationError !== undefined) {
            setValidationError(undefined);
        }
        
        setToggleModal(false);
    }

    let checkedNodeIds = [];

    const [groupCheckedIds, setGroupCheckedIds] = useState(checkedNodeIds);

    const handleChangeReportingPlace = ({ node, type, index }) => {

        setNodes(replaceNodeFromTree(nodes, node));

        if (type === 2) {
            if (node.state.selected === true) {
                if (!groupCheckedIds.includes(node.id)) {
                    setGroupCheckedIds([...groupCheckedIds, node.id]);
                }
            } else {
                if (groupCheckedIds.includes(node.id)) {
                    checkedNodeIds = groupCheckedIds.filter((value, index) => { return (value !== node.id) });
                    setGroupCheckedIds(checkedNodeIds);
                }
            }
        }
    }

    useEffect(() => {
        let fetchedNodes = [];
        if (groupTree !== undefined) {
            fetchedNodes = getFlattenedTree(groupTree);
        }
        let checkedNodeIds = [];
        fetchedNodes.map((value, index) => {
            if (value.state !== undefined && value.state.selected === true) {
                checkedNodeIds.push(value.id);
            }
        });
        setGroupCheckedIds(checkedNodeIds);
    }, [groupTree]);

    const handleChangeUserName = (userName) => {
        if (userName !== null) {
            setValidationError(undefined);
            setUserName(userName.value);
        } else {
            setValidationError(undefined);
            setEditDelegatedUser('');
            setUserName('');
        }
    }

    useEffect(() => {
        if (userName) {
            if (delegationUsers) {
                delegationUsers.map((value, index) => {
                    if (value.text === userName) {
                        setSelectedUserId(value.id);
                    }
                })
            }
        }
    }, [userName]);

    const handleChangeStartingDate = (date) => {
        setValidationError(undefined);
        setStartingDate(date);
    }

    const handleChangeEndingDate = (date) => {
        setValidationError(undefined);
        setEndingDate(date);
    }

    useEffect(() => {
        if (delegateRole !== '') {
            if (delegationDetails !== undefined &&
                delegationDetails.roles !== undefined &&
                delegationDetails.roles.length !== 0
                ) {
                    delegationDetails.roles.map((value, index) => {
                        if (value.role.name === delegateRole) {
                            delegationsActions.loadDelegationUsers(value.id, userName);
                        }
                    });
            }
        }
    }, [delegateRole]);

    const [reportingPlaceParam, setReportingPlaceParam] = useState(undefined);

    useEffect(() => {
        if (delegationDetails !== undefined && delegationDetails.delegation !== undefined) {
            setReportingPlaceParam(delegationDetails.delegation);
        }
    }, [delegationDetails]);

    useEffect(() => {
        if (partialRole.sourceUserRole !== undefined && partialRole.destinationUserRole !== undefined) {
            setReportingPlaceParam(partialRole);
        }
    }, [partialRole]);

    useEffect(() => {

        if (reportingPlaceParam !== undefined &&
            reportingPlaceParam.sourceUserRole !== undefined &&
            reportingPlaceParam.destinationUserRole !== undefined
        ) {
            delegationsActions.loadGroupTree(reportingPlaceParam.sourceUserRole.id, reportingPlaceParam.destinationUserRole.id);
        }
    }, [reportingPlaceParam]);

    useEffect(() => {
        setNodes(groupTree);
    }, [groupTree]);

    const handleChangeDelegateRole = (delegateRole, delegationDetails) => {
        if (delegateRole !== null &&
            delegationDetails !== undefined &&
            delegationDetails.roles !== undefined
        ) {
            delegationDetails.roles.map((value, index) => {
                if (value.role.name === delegateRole.value) {
                    delegationsActions.getDependsOnSelectedRolePartial(value.id);
                }
            });
            setDelegateRole(delegateRole.value);
            setValidationError(undefined);
            setUserName('');
        } else {
            if (delegationDetails !== undefined &&
                delegationDetails.roles !== undefined
            ) {
                delegationDetails.roles.map((value, index) => {
                    if (index === 0) {
                        delegationsActions.getDependsOnSelectedRolePartial(value.id);
                        setDelegateRole(value.role.name);
                    }
                });
            }
        }
        setValidationError(undefined);
        setUserName('');
    }

    const addDelegation = (event) => {

        setNodes(groupTree);

        if (delegationDetails !== undefined &&
            delegationDetails.roles !== undefined
        ) {
            delegationDetails.roles.map((value, index) => {
                if (index === 0) {
                    delegationsActions.getDependsOnSelectedRolePartial(value.id);
                    setDelegateRole(value.role.name);
                }
            });
        }

        setUserName('');
        setStartingDate(undefined);
        setEndingDate(undefined);
        setToggleModal(true);
    }

    const registerChild = useRef(null);

    const assignDelegation = (registerChild) => {
        
        if (reportingPlaceParam !== undefined &&
            selectedUserId !== undefined &&
            startingDate !== undefined &&
            endingDate !== undefined &&
            groupCheckedIds.length !== 0 &&
            userName !== '')
        {
            delegationsActions.saveDelegation(
                reportingPlaceParam.sourceUserRole.id,
                reportingPlaceParam.id,
                selectedUserId,
                moment(startingDate._d).format('YYYY-MM-DD'),
                moment(endingDate._d).format('YYYY-MM-DD'),
                groupCheckedIds);

            if (registerChild !== null &&
                registerChild !== undefined &&
                registerChild.current !== null
            ) {
                registerChild.current.forceUpdateGrids();
            }

            if (userName !== '') {
                setUserName('');
            }
            
            setStartingDate(undefined);
            setEndingDate(undefined);
            setToggleModal(false);

        } else {
            setValidationError(p.t("delegations_list_form_validation_error"));
        }
        
    }

    const editDelegation = (rowData, editIndex, parentGrid, event) => {

        setNodes(groupTree);

        delegationsIds.map((value, index) => {
            if (editIndex === index) {
                setEditIndex(value);
                delegationsActions.loadDelegationDetails(value);
            }
        });
        
        setDelegateRole(rowData[1]);
        setEditDelegatedUser(rowData[2]);

        if (rowData[3] !== 'Invalid date') {
            setStartingDate(moment(rowData[3]));
        } else {
            setStartingDate(undefined);
        }

        if (rowData[4] !== 'Invalid date') {
            setEndingDate(moment(rowData[4]));
        } else {
            setEndingDate(undefined);
        }

        setGridRef(parentGrid);
        setToggleModal(true);
    }

    useEffect(() => {

        let delegatedUser = "";

        if (editDelegatedUser !== "" &&
            delegationUsers !== undefined &&
            delegationUsers.length !== 0
            ) {
                delegationUsers.map((value, index) => {
                    if (value.text.includes(editDelegatedUser)) {
                        delegatedUser = value.text; 
                    }
                });
        }

        setUserName(delegatedUser);
    }, [editDelegatedUser, delegationUsers]);

    const updateDelegation = (selectedIndex, gridRef) => {
        
        if (reportingPlaceParam !== undefined &&
            selectedIndex !== null &&
            selectedUserId !== undefined &&
            startingDate !== null &&
            endingDate !== null &&
            groupCheckedIds.length !== 0)
        {
            delegationsActions.saveDelegation(
                reportingPlaceParam.sourceUserRole.id,
                selectedIndex,
                selectedUserId,
                moment(startingDate._d).format('YYYY-MM-DD'),
                moment(endingDate._d).format('YYYY-MM-DD'),
                groupCheckedIds);

            gridRef.forceUpdateGrids();

            if (editDelegatedUser !== '') {
                setEditDelegatedUser('');
            }

            if (editIndex !== null) {
                setEditIndex(null);
            }

            if (userName !== '') {
                setUserName('');
            }

            setStartingDate(undefined);
            setEndingDate(undefined);
            setToggleModal(false);

        } else {
            setValidationError(p.t("delegations_list_form_validation_error"));
        }

    }

    const deleteDelegation = (removeIndex, parentGrid, event) => {

        delegationsIds.map((value, index) => {
            if (removeIndex === index) {
                setDeleteIndex(value);
            }
        });

        setGridRef(parentGrid);
        setToggleModal(true);
    }

    const removeDelegation = (removeIndex, event) => {

        delegationsActions.deleteDelegation(removeIndex);

        gridRef.forceUpdateGrids();

        if (deleteIndex !== null) {
            setDeleteIndex(null);
        }

        setToggleModal(false);

    }

    const noContentRenderer = () => {

        return (
            <div className="delegations-grid-no-rows grid-message loading-message">
                <FadeUp ready={true}>
                    <h3>
                        <span className="message-icon-wrapper">
                            <i className="fa fa-exclamation-circle" />
                        </span>
                        {p.t("delegations_list_empty_grid")}
                    </h3>
                </FadeUp>
            </div>
            );
    }

    const cellRenderer = ({ columnIndex, key, parent, rowIndex, style }) => {
        
        return (
            <div key={key}
                style={{
                    ...style,
                    borderBottom: '1px solid #ddd',
                    textAlign: 'center',
                    paddingTop: '0.5%'
                }}
            >
                {(delegations[rowIndex][columnIndex] === "") ?
                        <div className="delegations-manager-buttons">
                            <button type="button" className="btn btn-sm edit-delegations-btn" onClick={(event) => editDelegation(delegations[rowIndex], rowIndex, parent, event)}>
                                <i className="fa fa-pencil edit-delegations-icon" aria-hidden="true"></i>
                            </button>
                            <button type="button" className="btn btn-sm remove-delegations-btn" onClick={(event) => deleteDelegation(rowIndex, parent, event)}>
                                <i className="fa fa-trash-o remove-delegations-icon" aria-hidden="true"></i>
                            </button>
                        </div>
                    :
                    (rowIndex === 0) ?
                        <div className="delegations-grid-header">
                            {delegations[rowIndex][columnIndex]}
                        </div>
                        :
                        <div className="delegations-grid-cell">
                            {delegations[rowIndex][columnIndex]}
                        </div>    
                    }
            </div>
        );
    }

    return (
        <div className="col-xs-12 delegations-list-container">
            <div className="delegations-list">
                <div className="row delegations-list-header">
                    <div className="col-lg-12 no-padding">
                        <div className="assign-delegations pull-right" onClick={(event) => addDelegation(event)}>
                            <i className="fa fa-plus assign-delegations-icon" aria-hidden="true"></i>
                        </div>
                    </div>
                    {toggleModal ?
                        <DelegationsModal
                            toggleModal={toggleModal}
                            rowIndex={rowIndex}
                            editIndex={editIndex}
                            deleteIndex={deleteIndex}
                            delegateRole={delegateRole}
                            handleChangeReportingPlace={handleChangeReportingPlace}
                            nodes={nodes}
                            userName={userName}
                            startingDate={startingDate}
                            endingDate={endingDate}
                            validationError={validationError}
                            handleChangeDelegateRole={handleChangeDelegateRole}
                            handleChangeUserName={handleChangeUserName}
                            handleChangeStartingDate={handleChangeStartingDate}
                            handleChangeEndingDate={handleChangeEndingDate}
                            assignDelegation={assignDelegation}
                            editDelegation={editDelegation}
                            updateDelegation={updateDelegation}
                            deleteDelegation={deleteDelegation}
                            removeDelegation={removeDelegation}
                            handleModalClose={handleModalClose}
                            gridRef={gridRef}
                            registerChild={registerChild}
                        />
                        :
                        null
                    }
                </div>
                <div className="delegations-list-wrapper">
                    <AutoSizer>
                        {({ height, width }) => (
                            <ColumnSizer
                                columnMaxWidth={325}
                                columnMinWidth={150}
                                columnCount={delegations[0].length}
                                key="GridColumnSizer"
                                width={width}
                            >
                                {({ adjustedWidth, columnWidth, registerChild }) => (
                                    <div
                                        className={styles.GridContainer}
                                        style={{
                                            height: 40,
                                            width: width,
                                    }}>
                                        <MultiGrid
                                            ref={registerChild}
                                            height={height}
                                            width={width}
                                            cellRenderer={cellRenderer}
                                            noContentRenderer={noContentRenderer}
                                            columnWidth={columnWidth}
                                            columnCount={delegations[0].length}
                                            rowHeight={rowHeight}
                                            rowCount={delegations.length}
                                            enableFixedRowScroll
                                            fixedRowCount={1}
                                            hideTopRightGridScrollbar
                                            enableFixedColumnScroll
                                            fixedColumnCount={(delegations.length > 1) ? 1 : 0}
                                            hideBottomLeftGridScrollbar
                                            styleBottomLeftGrid={{ borderRight: '1px solid #000000' }}
                                            styleTopLeftGrid={{ borderRight: '1px solid #000000' }}
                                />
                                    </div>
                                )}
                            </ColumnSizer>
                        )}
                    </AutoSizer>
                </div>
            </div>
        </div>
    );
}

DelegationsList.propTypes = {
    p: PropTypes.object,
    list: PropTypes.array,
    delegationDetails: PropTypes.object,
    groupTree: PropTypes.array,
    partialRole: PropTypes.object,
    delegationUsers: PropTypes.array
};

function mapStateToProps(state) {
    return {
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

export default connect(mapStateToProps, mapDispatchToProps)(translate(DelegationsList));