import classnames from 'classnames';
import { SelectHTMLAttributes } from 'react';

type Props = SelectHTMLAttributes<HTMLSelectElement>;

export default function SelectElement(props: Props) {
    return (
        <select {...props} className={classnames('govuk-select', props.className)}>
            {props.children}
        </select>
    );
}
