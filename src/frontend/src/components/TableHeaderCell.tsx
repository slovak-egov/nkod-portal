import { HTMLAttributes } from "react";
import { useTranslation } from "react-i18next";

type Props = 
{
    enableSorting: boolean;
    sortingDirection: "asc" | "desc" | null;
    toggleSortingDirection?: () => void;
} & HTMLAttributes<HTMLTableCellElement>

export default function TableHeaderCell(props: Props)
{
    let directionClass = '';
    if (props.sortingDirection === "asc") directionClass = "aes";
    if (props.sortingDirection === "desc") directionClass = "des";
    const {t} = useTranslation();

    return <th className="idsk-table__header">
        <span className="th-span">
            <>
            {props.children}
            {props.enableSorting ? <><button className={"arrowBtn " + directionClass} onClick={props.toggleSortingDirection}><span className="sr-only">{t('sortingByColumn')}</span></button></> : null }
            </>
        </span>
    </th>
}

TableHeaderCell.defaultProps = {
    enableSorting: false,
    sortingDirection: null
};