import React from "react";
import _ from "lodash";
import translate from "redux-polyglot/translate";
import ReportingPlaceTreeView from "./ReportingPlaceTree/ReportingPlaceTreeView";
import ReportingPlaceExpandable from "./ReportingPlaceTree/ReportingPlaceExpandable";
import ReportingPlaceSelection from "./ReportingPlaceTree/ReportingPlaceSelection";
import { getFlattenedTree } from "react-virtualized-tree/lib/selectors/getFlattenedTree";
import cn from "classnames";

class DelegationsReportingPlace extends React.Component {
    constructor(props) {
        super(props);

        this.state = {
            treeHeight: 0,
            loadingGroupManagersForNodeId: null
        };
    }

    render() {
        const { nodes, p, isLoading, isLoadingGroupTree } = this.props;
        const flattenedTree = getFlattenedTree(nodes);
        const nodeMarginLeft = 20;
        const nodeWidth = 870;

        return (
            <div className="delegations-reportingPlace-tree-container">
                <div className="delegations-reportingPlace-titel">{p.t("delegations_list_reporting_place")}</div>
                <div className="row delegations-reportingPlace-tree-wrapper">
                    {(isLoadingGroupTree) ?
                        <div className="grid-message loading-message">
                            <div className="bg-fadeup-transition">
                                <h3>
                                    <span className="loader-spinner-wrapper message-icon-wrapper">
                                        <i className="loader-spinner" />
                                    </span>
                                    {p.t("delegations_list_loading_reporting_places")}
                                </h3>
                            </div>
                        </div>
                        :
                        <div ref={this.wrapper} className="delegations-reportingPlace-tree">
                            <ReportingPlaceTreeView
                                nodes={flattenedTree}
                                onChange={this.props.handleChangeReportingPlace}
                            >
                                {({ style: s, node, ...rest }) => {
                                    const marginLeft = node.deepness * nodeMarginLeft + 10;
                                    const calcNodeWidth = nodeWidth - (marginLeft + node.deepness * 23);
                                    const style = { ...s, paddingRight: nodeWidth - calcNodeWidth, width: calcNodeWidth };

                                    return (
                                        <div
                                            className={cn("checkbox-tree-node", { "is-disabled": node.state.disabled })}
                                            style={style}
                                        >
                                            {/* Visual vertical tree lines  */}
                                            {Array.from({ length: node.deepness }).map((_, index) =>
                                                React.createElement(
                                                    "div",
                                                    { key: index, className: `vertical-separator` },
                                                    null
                                                )
                                            )}
                                            <ReportingPlaceExpandable node={node} {...rest}>
                                                <ReportingPlaceSelection node={node} {...rest} p={p} />
                                            </ReportingPlaceExpandable>
                                        </div>
                                    );
                                }}
                            </ReportingPlaceTreeView>
                        </div>
                    }
                </div>
            </div>
        );
    }
}

export default translate(DelegationsReportingPlace);
