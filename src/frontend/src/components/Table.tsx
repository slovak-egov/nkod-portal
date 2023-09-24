import { HTMLAttributes } from "react";
import IdSkModule from "./IdSkModule";

type Props = HTMLAttributes<HTMLTableElement>

export default function Table(props: Props)
{
    return <IdSkModule moduleType="idsk-table"><table className="idsk-table">
        {props.children}
    </table></IdSkModule>;
}