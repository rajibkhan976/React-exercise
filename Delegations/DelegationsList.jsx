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
import DelegatedUserInfo from "./DelegatedUserInfo";

const DelegationsList = ({ p, isLoading, isLoadingGroupTree, list, delegationDetails, delegableRoles, groupTree, partialRole, delegationUsers, delegatedUser, delegableUsers, delegationsActions }) => {

    const [delegateRole, setDelegateRole] = useState('');
    const [nodes, setNodes] = useState(undefined);
    const [userName, setUserName] = useState('');
    const [startingDate, setStartingDate] = useState(undefined);
    const [endingDate, setEndingDate] = useState(undefined);
    const [toggleModal, setToggleModal] = useState(false);
    const initialDelegations = [
        [   " ",
            p.t("delegations_list_header_role"),
            p.t("delegations_list_header_user"),
            p.t("delegations_list_header_starting_from"),
            p.t("delegations_list_header_ending_by")
        ]
    ];

    const [delegations, setDelegations] = useState(initialDelegations);
    const [delegationsIds, setDelegationsIds] = useState([]);

    useEffect(() => {
        let delegationsArray = [
            [   " ",
                p.t("delegations_list_header_role"),
                p.t("delegations_list_header_user"),
                p.t("delegations_list_header_starting_from"),
                p.t("delegations_list_header_ending_by")
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
    const [headerRowHeight, setHeaderRowHeight] = useState(30);
    const [rowHeight, setRowHeight] = useState(41);
    const [gridRef, setGridRef] = useState(undefined);
    const [validationError, setValidationError] = useState(undefined);

    const handleModalClose = () => {

        delegationsActions.resetDelegableRolesToPrevState();

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
                            delegationsActions.resetDelegableUsersToPrevState();
                            delegationsActions.loadDelegationUsers(value.id, userName);
                        }
                    });
            }
            if (reportingPlaceParam !== undefined &&
                reportingPlaceParam.sourceUserRole !== undefined &&
                reportingPlaceParam.destinationUserRole !== undefined
            ) {
                delegationsActions.loadAllowedGroupTree(reportingPlaceParam.sourceUserRole.id);
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

    const handleChangeDelegateRole = (delegateRole, delegationDetails, delegableRoles) => {
        if (delegateRole !== null &&
            delegationDetails !== undefined &&
            delegationDetails.roles !== undefined
        ) {
            delegationDetails.roles.map((value, index) => {
                if (value.role.name === delegateRole.value) {
                    delegationsActions.getDependsOnSelectedRolePartial(value.id);
                }
            });
            delegationsActions.resetDelegableUsersToPrevState();
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
        if (delegableRoles !== undefined &&
            delegableRoles !== null &&
            delegableRoles.length !== 0 &&
            delegateRole !== null) {
            delegableRoles.map((value, index) => {
                if (value.name === delegateRole.value) {
                    delegationsActions.resetDelegationUsersToPrevState();
                    setDelegateRole(delegateRole.value);
                    delegationsActions.loadDelegableUsers(value.id);
                }
            });
            setValidationError(undefined);
            setUserName('');
        } 
        setValidationError(undefined);
        setUserName('');
    }

    const addDelegation = (event) => {

        if (delegationDetails !== undefined &&
            delegationDetails.roles !== undefined &&
            delegationDetails.roles !== null &&
            delegationDetails.roles.length !== 0) {
            delegationDetails.roles.map((value, index) => {
                delegationsActions.loadDelegableRoles(value.roleId);
            });
        }

        if (isLoading) {
            window.loader.show();
        } else {
            window.loader.hide();

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
            //setNodes(groupTree);
            setToggleModal(true);
        }
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

            delegationsActions.resetDelegableRolesToPrevState();
            
            setStartingDate(undefined);
            setEndingDate(undefined);
            setToggleModal(false);

        } else {
            setValidationError(p.t("delegations_list_form_validation_error"));
        }
        
    }

    const editDelegation = (rowData, editIndex, parentGrid, event) => {

        if (isLoading) {
            window.loader.show();
        } else {
            window.loader.hide();

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
            //setNodes(groupTree);
            setGridRef(parentGrid);
            setToggleModal(true);
        }
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

    const rowHeightSetter = ({ index }) => {
        if (index === 0) {
            return headerRowHeight;
        } else {
            return rowHeight;
        }
    }

    const [columnWidth, setColumnWidth] = useState(undefined);

    const columnWidthGetter = (columnWidth) => {

        setColumnWidth(columnWidth);
    }

    const columnWidthSetter = ({ index }) => {
        if (index === 0) {
            return 82;
        } else {
            if (columnWidth !== undefined) {
                return columnWidth * 1.2;
            }
        }
    }

    const setRowClassName = (rowIndex) => {
        if (rowIndex !== 0) {
            return 'delegation-list-row-data';
        }
    }

    const noContentRenderer = () => {

        if (isLoading) {
            return (
                <div className="grid-message loading-message">
                    <div className="bg-fadeup-transition">
                        <h3>
                            <span className="loader-spinner-wrapper message-icon-wrapper">
                                <i className="loader-spinner" />
                            </span>
                            {p.t("delegations_list_loading_delegations")}
                        </h3>
                    </div>
                </div>
                );
        } else {
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
    }

    const renderLeftColumnCell = ({ columnIndex, key, parent, rowIndex, style }) => {

        return (
            <div className={"delegations-list-left-grid"} key={key} style={{ ...style, borderBottom: '1px solid #ddd' }}>
                {(rowIndex !== 0) ?
                    <div className="delegations-manager-buttons">
                        <button type="button" className="edit-delegations-btn" onClick={(event) => editDelegation(delegations[rowIndex], rowIndex, parent, event)}>
                            <i className="fa fa-pencil edit-delegations-icon" aria-hidden="true"></i>
                        </button>
                        <button type="button" className="remove-delegations-btn" onClick={(event) => deleteDelegation(rowIndex, parent, event)}>
                            <i className="fa fa-trash-o remove-delegations-icon" aria-hidden="true"></i>
                        </button>
                    </div>
                    :
                    null
                }
            </div>
            );
    }

    const renderHeaderCell = ({ columnIndex, key, parent, rowIndex, style }) => {
        return (
            <div className={"delegations-list-header-grid"}
                key={key}
                style={{
                    ...style,
                    paddingLeft: '4em'
                }}
            >
                <div className="delegations-grid-header-cell">
                    {delegations[rowIndex][columnIndex]}
                </div>
            </div>
            );
    }

    const [isOpenedDelegatedUserInfo, setIsOpenedDelegatedUserInfo] = useState([]);
    const [delegatedUserInfoIndex, setDelegatedUserInfoIndex] = useState(-1);
    const [dimensions, setDimensions] = useState({ top: 0, left: 0 });

    const openDelegatedUserInfo = (userName, rowIndex, event) => {
        event.stopPropagation();
        if (userName !== null && rowIndex !== null && list !== undefined && list.length !== 0) {
            list.map((value, index) => {
                if (userName === value.destinationUserRole.user.fullName && index === (rowIndex - 1)) {
                    delegationsActions.findDelegatedUser(value.destinationUserRole.userId);
                    setDelegatedUserInfoIndex(rowIndex);
                    setIsOpenedDelegatedUserInfo([rowIndex]);
                    let dimensions = placeDelegatedUserInfo(event);
                    setDimensions(dimensions);
                }
            });
        } else {
            closeDelegatedUserInfo();
        }
    }

    const closeDelegatedUserInfo = () => {
        setIsOpenedDelegatedUserInfo([]);
    }

    const placeDelegatedUserInfo = (event) => {
        let targetsDimension = event.target.getBoundingClientRect();
        let leftOffset = targetsDimension.left;
        let topOffset = targetsDimension.top - (targetsDimension.height * 3);
        return {
            top: topOffset,
            left: leftOffset
        };
    }

    const [delegatedUserName, setDelegatedUserName] = useState('');
    const [delegatedUserPhone, setDelegatedUserPhone] = useState('');
    const [delegatedUserEmail, setDelegatedUserEmail] = useState('');
    const [delegatedUserEmployment, setDelegatedUserEmployment] = useState([]);

    useEffect(() => {
        if (delegatedUser !== undefined && delegatedUser.length !== 0) {
            delegatedUser.map((value, index) => {
                if (value.persons !== undefined && value.persons.length !== 0) {
                    setDelegatedUserName(value.fullName);
                    setDelegatedUserPhone(value.phoneNumber);
                    setDelegatedUserEmail(value.email);
                    setDelegatedUserEmployment(value.persons);
                }
            })
        }
    }, [delegatedUser]);

    const renderBodyCell = ({ columnIndex, key, parent, rowIndex, style }) => {

        const className = setRowClassName(rowIndex);

        return (
            <div
                className={className}
                key={key}
                style={{
                    ...style,
                    borderBottom: '1px solid #ddd',
                    textAlign: 'left',
                    paddingTop: '0.2em',
                    paddingLeft: '4em'
                }}
            >
                {(columnIndex === 2) ?
                    <div className="delegations-grid-cell">
                        <span
                            className="delegated-user-name text-button"
                            onClick={(event) => openDelegatedUserInfo(delegations[rowIndex][columnIndex], rowIndex, event)}
                        >
                            <i className="fa fa-info-circle sub-icon" aria-hidden="true"></i>
                            {delegations[rowIndex][columnIndex]}
                        </span>
                    </div>
                    :
                    <div className="delegations-grid-cell">
                        {delegations[rowIndex][columnIndex]}
                    </div>
                }
            </div>
            );
    }

    const cellRenderer = ({ columnIndex, key, parent, rowIndex, style }) => {

        if (columnIndex === 0) {
            return renderLeftColumnCell({ columnIndex, key, parent, rowIndex, style });
        } else if (rowIndex === 0) {
            return renderHeaderCell({ columnIndex, key, parent, rowIndex, style });
        } else {
            return renderBodyCell({ columnIndex, key, parent, rowIndex, style });
        }
    }
    
    return (
        <div className="col-xs-12 delegations-list-container" onClick={(event) => openDelegatedUserInfo(null, null, event)}>
            <div className="delegations-list">
                <div className="row delegations-list-header">
                    <div className="col-lg-12 no-padding">
                        <div className="delegations-list-header-titel pull-left">
                            {p.t("delegations_list_header_manage_delegation")}
                        </div>
                        <div className="assign-delegations pull-right" onClick={(event) => addDelegation(event)}>
                            <i className="fa fa-plus assign-delegations-icon" aria-hidden="true"></i>
                        </div>
                    </div>
                    {toggleModal ?
                        <DelegationsModal
                            isLoading={isLoading}
                            isLoadingGroupTree={isLoadingGroupTree}
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
                            delegatedUserEmployment={delegatedUserEmployment}
                        />
                        :
                        null
                    }
                </div>
                {isOpenedDelegatedUserInfo ?
                    <DelegatedUserInfo
                        dimensions={dimensions}
                        delegatedUserName={delegatedUserName}
                        delegatedUserPhone={delegatedUserPhone}
                        delegatedUserEmail={delegatedUserEmail}
                        delegatedUserEmployment={delegatedUserEmployment}
                        rowIndex={delegatedUserInfoIndex}
                        isOpenedDelegatedUserInfo={isOpenedDelegatedUserInfo}
                        closeDelegatedUserInfo={closeDelegatedUserInfo}
                    />
                    :
                    null
                }
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
                                {({ adjustedWidth, columnWidth, registerChild }) => {

                                    columnWidthGetter(columnWidth);

                                    return (
                                        <div
                                            className={styles.GridContainer}
                                            style={{
                                                height: 30,
                                                width: width,
                                            }}>
                                            <MultiGrid
                                                ref={registerChild}
                                                height={height}
                                                width={width}
                                                cellRenderer={cellRenderer}
                                                noContentRenderer={noContentRenderer}
                                                columnWidth={columnWidthSetter}
                                                columnCount={delegations[0].length}
                                                rowHeight={rowHeightSetter}
                                                rowCount={delegations.length}
                                                enableFixedRowScroll
                                                fixedRowCount={1}
                                                hideTopRightGridScrollbar
                                                enableFixedColumnScroll
                                                fixedColumnCount={(delegations.length > 1) ? 1 : 0}
                                                hideBottomLeftGridScrollbar
                                                styleBottomLeftGrid={{ borderRight: '1px solid #000000', overflow: 'hidden' }}
                                                styleTopLeftGrid={{ borderRight: '1px solid #000000', borderBottom: 'none' }}
                                                styleTopRightGrid={{ overflow: 'hidden', borderBottom: 'none', borderLeft: 'none' }}
                                                styleBottomRightGrid={{ borderLeft: 'none' }}
                                            />
                                        </div>
                                    )
                                }}
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
    isLoading: PropTypes.bool,
    isLoadingGroupTree: PropTypes.bool,
    list: PropTypes.array,
    delegationDetails: PropTypes.object,
    groupTree: PropTypes.array,
    partialRole: PropTypes.object,
    delegationUsers: PropTypes.array,
    delegatedUser: PropTypes.array,
    delegableUsers: PropTypes.array
};

function mapStateToProps(state) {
    return {
        isLoading: state.delegations.myDelegations.isLoading,
        isLoadingGroupTree: state.delegations.myDelegations.isLoadingGroupTree,
        list: state.delegations.myDelegations.list,
        delegationDetails: state.delegations.myDelegations.delegationDetails,
        delegableRoles: state.delegations.myDelegations.delegableRoles,
        groupTree: state.delegations.myDelegations.groupTree,
        partialRole: state.delegations.myDelegations.partialRole,
        delegationUsers: state.delegations.myDelegations.delegationUsers,
        delegatedUser: state.delegations.myDelegations.delegatedUser,
        delegableUsers: state.delegations.myDelegations.delegableUsers
    };
}

function mapDispatchToProps(dispatch) {
    return {
        delegationsActions: bindActionCreators(delegationsActions, dispatch)
    };
}

export default connect(mapStateToProps, mapDispatchToProps)(translate(DelegationsList));