import { HTMLAttributes } from 'react';
import IdSkModule from './IdSkModule';

type Props = HTMLAttributes<HTMLTableElement> & {
    filter?: React.ReactNode;
};

export default function Table(props: Props) {
    return (
        <IdSkModule moduleType="idsk-table">
            {props.filter}
            <table className="idsk-table">{props.children}</table>
        </IdSkModule>
    );
}
