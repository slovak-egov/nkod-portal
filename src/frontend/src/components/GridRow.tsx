import classNames from 'classnames';
import { HTMLAttributes } from 'react';

type Props = HTMLAttributes<HTMLDivElement>;

export default function GridRow(props: Props) {
    const { className, children, ...rest } = props;

    return (
        <div className={classNames('govuk-grid-row', className)} {...rest}>
            {children}
        </div>
    );
}
