import { HTMLAttributes } from "react";

type Props = HTMLAttributes<HTMLTableSectionElement>

export default function TableHead(props: Props)
{
    return <thead className="idsk-table__head">
        {props.children}
    </thead>
}