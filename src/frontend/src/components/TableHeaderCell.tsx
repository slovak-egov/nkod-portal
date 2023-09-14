import { HTMLAttributes } from "react";

interface IProps extends HTMLAttributes<HTMLTableCellElement>
{
    enableSorting: boolean;
    sortingDirection: "asc" | "desc" | null;
    toggleSortingDirection?: () => void;
}

export default function TableHeaderCell(props: IProps)
{
    let directionClass = '';
    if (props.sortingDirection === "asc") directionClass = "aes";
    if (props.sortingDirection === "desc") directionClass = "des";

    return <th className="idsk-table__header">
        <span className="th-span">
            {props.children}
            {props.enableSorting ? <><button className={"arrowBtn " + directionClass} onClick={props.toggleSortingDirection}><span className="sr-only">Nezoradený stĺpec - použije vzostupné zoradenie.</span></button></> : null }
        </span>
    </th>
}

TableHeaderCell.defaultProps = {
    enableSorting: false,
    sortingDirection: null
};