import React, { useState, useEffect } from "react";
import m from "moment";
import translate from "redux-polyglot/translate";
import Collapse from "react-collapse";

const DelegatedUserInfo = ({
    p,
    rowIndex,
    isOpenedDelegatedUserInfo,
    closeDelegatedUserInfo,
    delegatedUserName,
    delegatedUserPhone,
    delegatedUserEmail,
    delegatedUserEmployment,
    dimensions
}) => {

    const [delegatedUserInfo, setDelegatedUserInfo] = useState([]);

    useEffect(() => {
        if (delegatedUserEmployment !== undefined && delegatedUserEmployment.length !== 0) {
            delegatedUserEmployment.map((value, index) => {
                setDelegatedUserInfo(value.employees);
            });
        }
    }, [delegatedUserEmployment]);

    const [employerCode, setEmployerCode] = useState('');
    const [employerName, setEmployerName] = useState('');
    const [workNumber, setWorkNumber] = useState('');
    const [employments, setEmployments] = useState([]);

    useEffect(() => {
        if (delegatedUserInfo !== undefined && delegatedUserInfo.length !== 0) {
            delegatedUserInfo.map((value, index) => {
                setEmployerCode(value.employerCode);
                setEmployerName(value.employerName);
                setWorkNumber(value.workNumber);
                setEmployments(value.employments);
            });
        }
    }, [delegatedUserInfo]);

    return (
        <div className="delegated-user-info-container" style={{ top: dimensions.top, left: dimensions.left }}>
            <Collapse isOpened={isOpenedDelegatedUserInfo.includes(rowIndex)}>
                <div className="delegated-user-info-wrapper">
                    <div className="delegated-user-info-header">
                        <div className="close-delegated-user-info-button pull-right" onClick={closeDelegatedUserInfo}>
                            <i className="fa fa-times" />
                        </div>
                    </div>
                    <div className="delegated-user-info-body">
                        <div className="delegated-user-personal-information">
                            <h1>{delegatedUserName}</h1>
                            <div className="delegated-user-contact-information">
                                {delegatedUserPhone &&
                                    <span className="phone-contact-icon">
                                        <i className="fa fa-phone" />
                                        {delegatedUserPhone}
                                    </span>}
                                {delegatedUserEmail &&
                                    <span className="mail-contact-icon">
                                        <i className="fa fa-envelope-o" />
                                        <a href={`mailto:${delegatedUserEmail}`}>{delegatedUserEmail}</a>
                                    </span>}
                            </div>
                        </div>
                        <div className="delegated-user-employment-information">
                            <h3>{p.t("delegations_list_user_employment")}</h3>
                            {(employments !== undefined && employments.length !== 0) ?
                                employments.map((value, index) => {
                                    return <fieldset key={index}>
                                                <div className="grid-row is-split">
                                                    <dl className="fieldtable">
                                                        <dt>{p.t("delegations_list_user_employer_number")}</dt>
                                                        <dd>{employerCode}</dd>

                                                        <dt>{p.t("delegations_list_user_employer")}</dt>
                                                        <dd>{employerName}</dd>

                                                        <dt>{p.t("delegations_list_user_employee_number")}</dt>
                                                        <dd>{workNumber}</dd>

                                                        <dt>{p.t("delegations_list_user_employment_designation")}</dt>
                                                        <dd>{value.position}</dd>

                                                        <dt>{p.t("delegations_list_user_employment_number")}</dt>
                                                        <dd>{value.employmentNumber}</dd>

                                                        <dt>{p.t("delegations_list_user_employment_duration")}</dt>
                                                        <dd>{`${value.startDate ? m(value.startDate).format("L") : ""} - ${value.endDate ? m(value.endDate).format("L") : ""}`}</dd>

                                                        <dt>{p.t("delegations_list_user_employment_accountpart")}</dt>
                                                        <dd>{value.accountPart}</dd>
                                                    </dl>
                                                </div>
                                    </fieldset>
                                })
                                :
                                null
                            }
                        </div>
                    </div>
                </div>
            </Collapse>
        </div>
        );
}

export default translate(DelegatedUserInfo);